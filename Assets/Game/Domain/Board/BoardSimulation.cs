using System;
using System.Collections.Generic;
using ThreeInARow.Domain.Commands;
using ThreeInARow.Domain.Events;
using ThreeInARow.Domain.Ids;
using ThreeInARow.Domain.Random;
using ThreeInARow.Domain.State;

namespace ThreeInARow.Domain.Board
{
    public enum SwapRejectionReason
    {
        None,
        BoardNotInitialized,
        CellOutOfBounds,
        CellsNotAdjacent,
        GemIsImmovable,
        SwapCreatesNoMatch
    }

    public sealed class BoardSwapResult
    {
        public readonly bool Accepted;
        public readonly SwapRejectionReason RejectionReason;
        public readonly EventBatch Events;
        public readonly int CascadeCount;
        public readonly bool Reshuffled;

        private BoardSwapResult(
            bool accepted,
            SwapRejectionReason rejectionReason,
            EventBatch events,
            int cascadeCount,
            bool reshuffled)
        {
            Accepted = accepted;
            RejectionReason = rejectionReason;
            Events = events;
            CascadeCount = cascadeCount;
            Reshuffled = reshuffled;
        }

        public static BoardSwapResult Reject(SwapRejectionReason reason)
        {
            return new BoardSwapResult(false, reason, new EventBatch(), 0, false);
        }

        public static BoardSwapResult Accept(EventBatch events, int cascadeCount, bool reshuffled)
        {
            return new BoardSwapResult(true, SwapRejectionReason.None, events, cascadeCount, reshuffled);
        }
    }

    public readonly struct LegalSwap
    {
        public readonly GridCell CellA;
        public readonly GridCell CellB;

        public LegalSwap(GridCell cellA, GridCell cellB)
        {
            CellA = cellA;
            CellB = cellB;
        }
    }

    /// <summary>
    /// Authoritative, scene-independent 7x7 board simulation. All accepted operations work on a copy
    /// and commit the board and BoardSpawn RNG together, so rejected swaps cannot partially change a run.
    /// </summary>
    public static class BoardSimulation
    {
        private const int MaximumCascades = 256;
        private const int MaximumReshuffleAttempts = 512;

        public static EventBatch InitializeBoard(RunState state, IBoardContentCatalog catalog = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            catalog = catalog ?? MvpBoardContentCatalog.Instance;
            ValidateCatalog(catalog);

            var random = RandomStreams.Restore(RandomStream.BoardSpawn, state.RandomStreams);
            var board = new BoardMatrix();
            var events = new EventBatch();
            events.Add(SimulationEventType.BoardInitialized, "system.board", "width=7;height=7");

            for (var row = 0; row < BoardState.Height; row++)
            {
                for (var column = 0; column < BoardState.Width; column++)
                {
                    var cell = new GridCell(column, row);
                    var gem = CreateInitialGem(board, cell, random, catalog);
                    board[column, row] = gem;
                    events.Add(SimulationEventType.GemSpawned, gem.GemId, "initial", 1, null, cell, gem.SpecialId);
                }
            }

            if (!HasAnyLegalSwap(board, catalog))
                Reshuffle(board, random, catalog, events, "initial_no_legal_swap");

            if (state.Board == null) state.Board = new BoardState();
            board.CommitTo(state.Board);
            RandomStreams.Store(RandomStream.BoardSpawn, random, state.RandomStreams);
            return events;
        }

        public static BoardSwapResult ResolveSwap(
            RunState state,
            SwapCommand command,
            IBoardContentCatalog catalog = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (command == null) throw new ArgumentNullException(nameof(command));
            catalog = catalog ?? MvpBoardContentCatalog.Instance;
            ValidateCatalog(catalog);

            if (state.Board == null || state.Board.Gems == null || state.Board.Gems.Count != BoardState.Width * BoardState.Height)
                return BoardSwapResult.Reject(SwapRejectionReason.BoardNotInitialized);
            if (!IsInBounds(command.CellA) || !IsInBounds(command.CellB))
                return BoardSwapResult.Reject(SwapRejectionReason.CellOutOfBounds);
            if (!AreAdjacent(command.CellA, command.CellB))
                return BoardSwapResult.Reject(SwapRejectionReason.CellsNotAdjacent);

            var board = BoardMatrix.FromState(state.Board);
            var gemA = board[command.CellA];
            var gemB = board[command.CellB];
            if (!IsMovable(gemA) || !IsMovable(gemB))
                return BoardSwapResult.Reject(SwapRejectionReason.GemIsImmovable);

            var prismA = IsPrism(gemA);
            var prismB = IsPrism(gemB);
            if (prismA && prismB)
                return BoardSwapResult.Reject(SwapRejectionReason.SwapCreatesNoMatch);
            if (prismA && !catalog.IsNormalGem(gemB.GemId))
                return BoardSwapResult.Reject(SwapRejectionReason.SwapCreatesNoMatch);
            if (prismB && !catalog.IsNormalGem(gemA.GemId))
                return BoardSwapResult.Reject(SwapRejectionReason.SwapCreatesNoMatch);

            board.Swap(command.CellA, command.CellB);
            var initialMatches = FindMatchGroups(board, catalog);
            if (!prismA && !prismB && !AnyGroupContains(initialMatches, command.CellA, command.CellB))
                return BoardSwapResult.Reject(SwapRejectionReason.SwapCreatesNoMatch);

            var events = new EventBatch();
            events.Add(
                SimulationEventType.SwapAccepted,
                "system.board",
                "valid player swap",
                0,
                command.CellA,
                command.CellB);

            var random = RandomStreams.Restore(RandomStream.BoardSpawn, state.RandomStreams);
            var cascadeCount = 0;

            if (prismA || prismB)
            {
                ResolvePrismSwap(board, command.CellA, command.CellB, random, catalog, events);
                cascadeCount++;
                ResolveCascades(board, random, catalog, events, ref cascadeCount, null, null);
            }
            else
            {
                ResolveCascades(
                    board,
                    random,
                    catalog,
                    events,
                    ref cascadeCount,
                    command.CellA,
                    command.CellB,
                    initialMatches);
            }

            var reshuffled = false;
            if (!HasAnyLegalSwap(board, catalog))
            {
                Reshuffle(board, random, catalog, events, "stable_no_legal_swap");
                reshuffled = true;
            }

            board.CommitTo(state.Board);
            RandomStreams.Store(RandomStream.BoardSpawn, random, state.RandomStreams);
            return BoardSwapResult.Accept(events, cascadeCount, reshuffled);
        }

        public static IReadOnlyList<LegalSwap> FindLegalSwaps(
            BoardState state,
            IBoardContentCatalog catalog = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            catalog = catalog ?? MvpBoardContentCatalog.Instance;
            ValidateCatalog(catalog);
            if (state.Gems == null || state.Gems.Count != BoardState.Width * BoardState.Height)
                return new List<LegalSwap>();

            return FindLegalSwaps(BoardMatrix.FromState(state), catalog);
        }

        /// <summary>
        /// Re-establishes the stable-board playability invariant after an external system changes
        /// movement statuses. It consumes BoardSpawn only when a reshuffle is actually required.
        /// </summary>
        public static EventBatch EnsurePlayable(RunState state, IBoardContentCatalog catalog = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            catalog = catalog ?? MvpBoardContentCatalog.Instance;
            ValidateCatalog(catalog);
            var events = new EventBatch();
            if (state.Board == null || state.Board.Gems == null ||
                state.Board.Gems.Count != BoardState.Width * BoardState.Height)
                return events;

            var board = BoardMatrix.FromState(state.Board);
            if (HasAnyLegalSwap(board, catalog)) return events;
            var random = RandomStreams.Restore(RandomStream.BoardSpawn, state.RandomStreams);
            Reshuffle(board, random, catalog, events, "status_application_no_legal_swap");
            board.CommitTo(state.Board);
            RandomStreams.Store(RandomStream.BoardSpawn, random, state.RandomStreams);
            return events;
        }

        private static void ResolvePrismSwap(
            BoardMatrix board,
            GridCell cellA,
            GridCell cellB,
            DeterministicRandom random,
            IBoardContentCatalog catalog,
            EventBatch events)
        {
            var gemA = board[cellA];
            var gemB = board[cellB];
            var prismCell = IsPrism(gemA) ? cellA : cellB;
            var targetGem = IsPrism(gemA) ? gemB : gemA;
            var clearCells = new List<GridCell>();

            for (var row = 0; row < BoardState.Height; row++)
            {
                for (var column = 0; column < BoardState.Width; column++)
                {
                    var cell = new GridCell(column, row);
                    var gem = board[cell];
                    if (cell.Equals(prismCell) || gem.GemId.Equals(targetGem.GemId)) clearCells.Add(cell);
                }
            }

            events.Add(
                SimulationEventType.SpecialActivated,
                BoardContentIds.Prism,
                "swap_color_clear",
                clearCells.Count - 1,
                prismCell,
                null,
                targetGem.GemId);
            ClearCells(board, clearCells, events, 1, prismCell);
            CollapseAndRefill(board, random, catalog, events, 1);
        }

        private static void ResolveCascades(
            BoardMatrix board,
            DeterministicRandom random,
            IBoardContentCatalog catalog,
            EventBatch events,
            ref int cascadeCount,
            GridCell? swapCellA,
            GridCell? swapCellB,
            List<MatchGroup> matches = null)
        {
            matches = matches ?? FindMatchGroups(board, catalog);
            while (matches.Count > 0)
            {
                cascadeCount++;
                if (cascadeCount > MaximumCascades)
                    throw new InvalidOperationException("Board resolution exceeded the cascade safety limit.");

                ResolveMatchStep(board, matches, catalog, events, cascadeCount, swapCellA, swapCellB);
                CollapseAndRefill(board, random, catalog, events, cascadeCount);
                swapCellA = null;
                swapCellB = null;
                matches = FindMatchGroups(board, catalog);
            }
        }

        private static void ResolveMatchStep(
            BoardMatrix board,
            List<MatchGroup> matches,
            IBoardContentCatalog catalog,
            EventBatch events,
            int cascade,
            GridCell? swapCellA,
            GridCell? swapCellB)
        {
            var creations = new List<SpecialCreation>();
            var clearCells = new HashSet<GridCell>();

            foreach (var match in matches)
            {
                events.Add(
                    SimulationEventType.GemsMatched,
                    match.GemId,
                    "cascade=" + cascade + ";pattern=" + match.Pattern,
                    match.Cells.Count,
                    match.Cells[0]);

                SpecialCreation creation;
                if (TryCreateSpecial(board, match, catalog, swapCellA, swapCellB, out creation))
                    creations.Add(creation);
                foreach (var cell in match.Cells) clearCells.Add(cell);
            }

            creations.Sort((left, right) => left.Cell.CompareTo(right.Cell));
            foreach (var creation in creations)
            {
                clearCells.Remove(creation.Cell);
                var existingStatuses = board[creation.Cell].StatusIds;
                var existingDurations = board[creation.Cell].StatusDurations;
                board[creation.Cell] = new BoardGemState
                {
                    Cell = creation.Cell,
                    GemId = creation.GemId,
                    SpecialId = creation.SpecialId,
                    StatusIds = existingStatuses == null
                        ? new List<ContentId>()
                        : new List<ContentId>(existingStatuses),
                    StatusDurations = CloneStatusDurations(existingDurations)
                };
                events.Add(
                    SimulationEventType.SpecialCreated,
                    creation.SpecialId,
                    "cascade=" + cascade,
                    1,
                    creation.Cell,
                    null,
                    creation.SourceGemId);
            }

            var orderedClearCells = new List<GridCell>(clearCells);
            orderedClearCells.Sort();
            ClearCells(board, orderedClearCells, events, cascade, null);
        }

        private static bool TryCreateSpecial(
            BoardMatrix board,
            MatchGroup match,
            IBoardContentCatalog catalog,
            GridCell? swapCellA,
            GridCell? swapCellB,
            out SpecialCreation creation)
        {
            ContentId specialId;
            ContentId resultingGemId;
            if (match.CreatesPrism)
            {
                specialId = BoardContentIds.Prism;
                resultingGemId = BoardContentIds.PrismGem;
            }
            else if (match.CreatesMatchFourSpecial)
            {
                specialId = catalog.GetMatchFourSpecial(match.GemId);
                resultingGemId = match.GemId;
            }
            else
            {
                creation = default(SpecialCreation);
                return false;
            }

            var cell = SelectCreationCell(board, match.Cells, swapCellA, swapCellB);
            creation = new SpecialCreation(cell, resultingGemId, specialId, match.GemId);
            return true;
        }

        private static GridCell SelectCreationCell(
            BoardMatrix board,
            List<GridCell> cells,
            GridCell? swapCellA,
            GridCell? swapCellB)
        {
            if (swapCellB.HasValue && cells.Contains(swapCellB.Value) && !HasSpecial(board[swapCellB.Value]))
                return swapCellB.Value;
            if (swapCellA.HasValue && cells.Contains(swapCellA.Value) && !HasSpecial(board[swapCellA.Value]))
                return swapCellA.Value;
            foreach (var cell in cells)
                if (!HasSpecial(board[cell])) return cell;
            return cells[0];
        }

        private static void ClearCells(
            BoardMatrix board,
            List<GridCell> cells,
            EventBatch events,
            int cascade,
            GridCell? prismActivationCell)
        {
            cells.Sort();
            foreach (var cell in cells)
            {
                var gem = board[cell];
                if (gem == null) continue;

                var statuses = gem.StatusIds == null
                    ? new List<ContentId>()
                    : new List<ContentId>(gem.StatusIds);
                foreach (var statusId in statuses)
                {
                    events.Add(
                        SimulationEventType.StatusRemoved,
                        statusId,
                        "cleared",
                        1,
                        cell,
                        null,
                        gem.GemId);
                }

                events.Add(
                    SimulationEventType.GemCleared,
                    gem.GemId,
                    "cascade=" + cascade,
                    1,
                    cell,
                    null,
                    NormalizeSpecialId(gem.SpecialId),
                    statuses);

                if (HasSpecial(gem) && (!prismActivationCell.HasValue || !cell.Equals(prismActivationCell.Value)))
                {
                    events.Add(
                        SimulationEventType.SpecialActivated,
                        gem.SpecialId,
                        "cleared",
                        1,
                        cell,
                        null,
                        gem.GemId);
                }

                board[cell] = null;
            }
        }

        private static void CollapseAndRefill(
            BoardMatrix board,
            DeterministicRandom random,
            IBoardContentCatalog catalog,
            EventBatch events,
            int cascade)
        {
            var moves = new List<GemMove>();
            var spawns = new List<BoardGemState>();

            for (var column = 0; column < BoardState.Width; column++)
            {
                var segmentStart = 0;
                for (var row = 0; row < BoardState.Height; row++)
                {
                    var gem = board[column, row];
                    if (gem == null || !HasStatus(gem, BoardContentIds.Anchored)) continue;
                    CollapseSegment(board, column, segmentStart, row - 1, random, catalog, moves, spawns);
                    segmentStart = row + 1;
                }
                CollapseSegment(board, column, segmentStart, BoardState.Height - 1, random, catalog, moves, spawns);
            }

            moves.Sort((left, right) =>
            {
                var comparison = left.To.CompareTo(right.To);
                return comparison != 0 ? comparison : left.From.CompareTo(right.From);
            });
            foreach (var move in moves)
            {
                events.Add(
                    SimulationEventType.GemMoved,
                    move.Gem.GemId,
                    "cascade=" + cascade,
                    1,
                    move.From,
                    move.To,
                    NormalizeSpecialId(move.Gem.SpecialId));
            }

            spawns.Sort((left, right) => left.Cell.CompareTo(right.Cell));
            foreach (var spawn in spawns)
            {
                events.Add(
                    SimulationEventType.GemSpawned,
                    spawn.GemId,
                    "cascade=" + cascade,
                    1,
                    null,
                    spawn.Cell,
                    spawn.SpecialId);
            }
        }

        private static void CollapseSegment(
            BoardMatrix board,
            int column,
            int startRow,
            int endRow,
            DeterministicRandom random,
            IBoardContentCatalog catalog,
            List<GemMove> moves,
            List<BoardGemState> spawns)
        {
            if (startRow > endRow) return;

            var writeRow = startRow;
            for (var readRow = startRow; readRow <= endRow; readRow++)
            {
                var gem = board[column, readRow];
                if (gem == null) continue;
                if (writeRow != readRow)
                {
                    var from = new GridCell(column, readRow);
                    var to = new GridCell(column, writeRow);
                    board[column, writeRow] = gem;
                    board[column, readRow] = null;
                    gem.Cell = to;
                    moves.Add(new GemMove(from, to, gem));
                }
                writeRow++;
            }

            while (writeRow <= endRow)
            {
                var cell = new GridCell(column, writeRow);
                var gem = CreateRandomGem(cell, random, catalog);
                board[cell] = gem;
                spawns.Add(gem);
                writeRow++;
            }
        }

        private static void Reshuffle(
            BoardMatrix board,
            DeterministicRandom random,
            IBoardContentCatalog catalog,
            EventBatch events,
            string reason)
        {
            var movableCells = new List<GridCell>();
            var movableGems = new List<BoardGemState>();
            for (var row = 0; row < BoardState.Height; row++)
            {
                for (var column = 0; column < BoardState.Width; column++)
                {
                    var cell = new GridCell(column, row);
                    var gem = board[cell];
                    if (!IsMovable(gem)) continue;
                    movableCells.Add(cell);
                    movableGems.Add(gem);
                }
            }

            for (var attempt = 1; attempt <= MaximumReshuffleAttempts; attempt++)
            {
                Shuffle(movableGems, random);
                for (var index = 0; index < movableCells.Count; index++) board[movableCells[index]] = movableGems[index];
                if (FindMatchGroups(board, catalog).Count != 0 || !HasAnyLegalSwap(board, catalog)) continue;
                NormalizeCells(board);
                events.Add(SimulationEventType.BoardReshuffled, "system.board", reason + ";mode=permutation", attempt);
                return;
            }

            for (var attempt = 1; attempt <= MaximumReshuffleAttempts; attempt++)
            {
                var complete = true;
                foreach (var cell in movableCells)
                {
                    var existingStatuses = board[cell].StatusIds;
                    var existingDurations = board[cell].StatusDurations;
                    ContentId gemId;
                    if (!TrySelectNonMatchingGem(board, cell, random, catalog, out gemId))
                    {
                        complete = false;
                        break;
                    }
                    board[cell] = new BoardGemState
                    {
                        Cell = cell,
                        GemId = gemId,
                        SpecialId = BoardContentIds.NoSpecial,
                        StatusIds = existingStatuses == null ? new List<ContentId>() : new List<ContentId>(existingStatuses),
                        StatusDurations = CloneStatusDurations(existingDurations)
                    };
                }

                if (!complete || FindMatchGroups(board, catalog).Count != 0 || !HasAnyLegalSwap(board, catalog)) continue;
                NormalizeCells(board);
                events.Add(SimulationEventType.BoardReshuffled, "system.board", reason + ";mode=regenerated", attempt);
                return;
            }

            throw new InvalidOperationException("Unable to create a stable, playable board while preserving immovable cells.");
        }

        private static void Shuffle(List<BoardGemState> gems, DeterministicRandom random)
        {
            for (var index = gems.Count - 1; index > 0; index--)
            {
                var other = random.NextInt(index + 1);
                var temporary = gems[index];
                gems[index] = gems[other];
                gems[other] = temporary;
            }
        }

        private static BoardGemState CreateInitialGem(
            BoardMatrix board,
            GridCell cell,
            DeterministicRandom random,
            IBoardContentCatalog catalog)
        {
            ContentId gemId;
            if (!TrySelectNonMatchingGem(board, cell, random, catalog, out gemId))
                throw new InvalidOperationException("The board catalog cannot produce a match-free initial board.");
            return NewGem(cell, gemId);
        }

        private static bool TrySelectNonMatchingGem(
            BoardMatrix board,
            GridCell cell,
            DeterministicRandom random,
            IBoardContentCatalog catalog,
            out ContentId gemId)
        {
            var start = random.NextInt(catalog.SpawnableGemIds.Count);
            for (var offset = 0; offset < catalog.SpawnableGemIds.Count; offset++)
            {
                var candidate = catalog.SpawnableGemIds[(start + offset) % catalog.SpawnableGemIds.Count];
                if (WouldCreateBackwardMatch(board, cell, candidate)) continue;
                gemId = candidate;
                return true;
            }

            gemId = default(ContentId);
            return false;
        }

        private static bool WouldCreateBackwardMatch(BoardMatrix board, GridCell cell, ContentId gemId)
        {
            if (cell.Column >= 2)
            {
                var first = board[cell.Column - 1, cell.Row];
                var second = board[cell.Column - 2, cell.Row];
                if (first != null && second != null && first.GemId.Equals(gemId) && second.GemId.Equals(gemId)) return true;
            }
            if (cell.Row >= 2)
            {
                var first = board[cell.Column, cell.Row - 1];
                var second = board[cell.Column, cell.Row - 2];
                if (first != null && second != null && first.GemId.Equals(gemId) && second.GemId.Equals(gemId)) return true;
            }
            return false;
        }

        private static BoardGemState CreateRandomGem(
            GridCell cell,
            DeterministicRandom random,
            IBoardContentCatalog catalog)
        {
            return NewGem(cell, catalog.SpawnableGemIds[random.NextInt(catalog.SpawnableGemIds.Count)]);
        }

        private static BoardGemState NewGem(GridCell cell, ContentId gemId)
        {
            return new BoardGemState
            {
                Cell = cell,
                GemId = gemId,
                SpecialId = BoardContentIds.NoSpecial,
                StatusIds = new List<ContentId>(),
                StatusDurations = new List<BoardStatusDurationState>()
            };
        }

        private static List<BoardStatusDurationState> CloneStatusDurations(List<BoardStatusDurationState> source)
        {
            var result = new List<BoardStatusDurationState>();
            if (source == null) return result;
            foreach (var item in source)
            {
                if (item == null) continue;
                result.Add(new BoardStatusDurationState
                {
                    StatusId = item.StatusId,
                    RemainingPlayerTurns = item.RemainingPlayerTurns
                });
            }
            return result;
        }

        private static IReadOnlyList<LegalSwap> FindLegalSwaps(BoardMatrix board, IBoardContentCatalog catalog)
        {
            var result = new List<LegalSwap>();
            for (var row = 0; row < BoardState.Height; row++)
            {
                for (var column = 0; column < BoardState.Width; column++)
                {
                    var cell = new GridCell(column, row);
                    if (column + 1 < BoardState.Width)
                    {
                        var right = new GridCell(column + 1, row);
                        if (IsLegalSwap(board, cell, right, catalog)) result.Add(new LegalSwap(cell, right));
                    }
                    if (row + 1 < BoardState.Height)
                    {
                        var above = new GridCell(column, row + 1);
                        if (IsLegalSwap(board, cell, above, catalog)) result.Add(new LegalSwap(cell, above));
                    }
                }
            }
            return result;
        }

        private static bool HasAnyLegalSwap(BoardMatrix board, IBoardContentCatalog catalog)
        {
            for (var row = 0; row < BoardState.Height; row++)
            {
                for (var column = 0; column < BoardState.Width; column++)
                {
                    var cell = new GridCell(column, row);
                    if (column + 1 < BoardState.Width && IsLegalSwap(board, cell, new GridCell(column + 1, row), catalog)) return true;
                    if (row + 1 < BoardState.Height && IsLegalSwap(board, cell, new GridCell(column, row + 1), catalog)) return true;
                }
            }
            return false;
        }

        private static bool IsLegalSwap(BoardMatrix board, GridCell cellA, GridCell cellB, IBoardContentCatalog catalog)
        {
            var gemA = board[cellA];
            var gemB = board[cellB];
            if (!IsMovable(gemA) || !IsMovable(gemB)) return false;
            var prismA = IsPrism(gemA);
            var prismB = IsPrism(gemB);
            if (prismA || prismB) return prismA != prismB && catalog.IsNormalGem(prismA ? gemB.GemId : gemA.GemId);

            board.Swap(cellA, cellB);
            var matches = FindMatchGroups(board, catalog);
            board.Swap(cellA, cellB);
            return AnyGroupContains(matches, cellA, cellB);
        }

        private static bool AnyGroupContains(List<MatchGroup> groups, GridCell cellA, GridCell cellB)
        {
            foreach (var group in groups)
                if (group.Cells.Contains(cellA) || group.Cells.Contains(cellB)) return true;
            return false;
        }

        private static List<MatchGroup> FindMatchGroups(BoardMatrix board, IBoardContentCatalog catalog)
        {
            var runs = new List<MatchRun>();
            for (var row = 0; row < BoardState.Height; row++)
            {
                var start = 0;
                while (start < BoardState.Width)
                {
                    var gem = board[start, row];
                    if (gem == null || !catalog.IsNormalGem(gem.GemId))
                    {
                        start++;
                        continue;
                    }
                    var end = start + 1;
                    while (end < BoardState.Width && board[end, row] != null && board[end, row].GemId.Equals(gem.GemId)) end++;
                    if (end - start >= 3)
                    {
                        var cells = new List<GridCell>();
                        for (var column = start; column < end; column++) cells.Add(new GridCell(column, row));
                        runs.Add(new MatchRun(gem.GemId, true, cells));
                    }
                    start = end;
                }
            }

            for (var column = 0; column < BoardState.Width; column++)
            {
                var start = 0;
                while (start < BoardState.Height)
                {
                    var gem = board[column, start];
                    if (gem == null || !catalog.IsNormalGem(gem.GemId))
                    {
                        start++;
                        continue;
                    }
                    var end = start + 1;
                    while (end < BoardState.Height && board[column, end] != null && board[column, end].GemId.Equals(gem.GemId)) end++;
                    if (end - start >= 3)
                    {
                        var cells = new List<GridCell>();
                        for (var row = start; row < end; row++) cells.Add(new GridCell(column, row));
                        runs.Add(new MatchRun(gem.GemId, false, cells));
                    }
                    start = end;
                }
            }

            var groups = new List<MatchGroup>();
            foreach (var run in runs)
            {
                var overlapping = new List<MatchGroup>();
                foreach (var group in groups)
                    if (group.Overlaps(run)) overlapping.Add(group);

                MatchGroup target;
                if (overlapping.Count == 0)
                {
                    target = new MatchGroup(run.GemId);
                    groups.Add(target);
                }
                else
                {
                    target = overlapping[0];
                    for (var index = 1; index < overlapping.Count; index++)
                    {
                        target.Merge(overlapping[index]);
                        groups.Remove(overlapping[index]);
                    }
                }
                target.Add(run);
            }

            foreach (var group in groups) group.FinalizeCells();
            groups.Sort((left, right) => left.Cells[0].CompareTo(right.Cells[0]));
            return groups;
        }

        private static bool IsMovable(BoardGemState gem)
        {
            return gem != null
                && !HasStatus(gem, BoardContentIds.Frozen)
                && !HasStatus(gem, BoardContentIds.Anchored);
        }

        private static bool HasStatus(BoardGemState gem, ContentId statusId)
        {
            if (gem == null || gem.StatusIds == null) return false;
            foreach (var existing in gem.StatusIds)
                if (existing.Equals(statusId)) return true;
            return false;
        }

        private static bool HasSpecial(BoardGemState gem)
        {
            return gem != null
                && !string.IsNullOrEmpty(gem.SpecialId.Value)
                && !gem.SpecialId.Equals(BoardContentIds.NoSpecial);
        }

        private static bool IsPrism(BoardGemState gem)
        {
            return gem != null && gem.SpecialId.Equals(BoardContentIds.Prism);
        }

        private static ContentId NormalizeSpecialId(ContentId specialId)
        {
            return string.IsNullOrEmpty(specialId.Value) ? BoardContentIds.NoSpecial : specialId;
        }

        private static bool IsInBounds(GridCell cell)
        {
            return cell.Column >= 0 && cell.Column < BoardState.Width && cell.Row >= 0 && cell.Row < BoardState.Height;
        }

        private static bool AreAdjacent(GridCell first, GridCell second)
        {
            return Math.Abs(first.Column - second.Column) + Math.Abs(first.Row - second.Row) == 1;
        }

        private static void ValidateCatalog(IBoardContentCatalog catalog)
        {
            if (catalog.SpawnableGemIds == null || catalog.SpawnableGemIds.Count < 3)
                throw new ArgumentException("A board catalog requires at least three spawnable gem IDs.", nameof(catalog));
            foreach (var gemId in catalog.SpawnableGemIds)
                if (!catalog.IsNormalGem(gemId))
                    throw new ArgumentException("Every spawnable gem must be a normal matchable gem.", nameof(catalog));
        }

        private static void NormalizeCells(BoardMatrix board)
        {
            for (var row = 0; row < BoardState.Height; row++)
                for (var column = 0; column < BoardState.Width; column++)
                    board[column, row].Cell = new GridCell(column, row);
        }

        private sealed class BoardMatrix
        {
            private readonly BoardGemState[,] _gems = new BoardGemState[BoardState.Width, BoardState.Height];

            public BoardGemState this[int column, int row]
            {
                get => _gems[column, row];
                set => _gems[column, row] = value;
            }

            public BoardGemState this[GridCell cell]
            {
                get => _gems[cell.Column, cell.Row];
                set => _gems[cell.Column, cell.Row] = value;
            }

            public void Swap(GridCell first, GridCell second)
            {
                var temporary = this[first];
                this[first] = this[second];
                this[second] = temporary;
            }

            public void CommitTo(BoardState state)
            {
                state.Gems.Clear();
                for (var row = 0; row < BoardState.Height; row++)
                {
                    for (var column = 0; column < BoardState.Width; column++)
                    {
                        var cell = new GridCell(column, row);
                        var gem = this[cell];
                        if (gem == null) throw new InvalidOperationException("A resolved board cannot contain an empty cell.");
                        gem.Cell = cell;
                        state.Gems.Add(CloneGem(gem));
                    }
                }
            }

            public static BoardMatrix FromState(BoardState state)
            {
                var result = new BoardMatrix();
                var occupied = new HashSet<GridCell>();
                foreach (var source in state.Gems)
                {
                    if (source == null) throw new InvalidOperationException("Board state contains a null gem.");
                    if (!IsInBounds(source.Cell)) throw new InvalidOperationException("Board state contains an out-of-bounds cell.");
                    if (!occupied.Add(source.Cell)) throw new InvalidOperationException("Board state contains a duplicate cell.");
                    result[source.Cell] = CloneGem(source);
                }
                if (occupied.Count != BoardState.Width * BoardState.Height)
                    throw new InvalidOperationException("Board state does not contain every grid cell.");
                return result;
            }

            private static BoardGemState CloneGem(BoardGemState source)
            {
                return new BoardGemState
                {
                    Cell = source.Cell,
                    GemId = source.GemId,
                    SpecialId = NormalizeSpecialId(source.SpecialId),
                    StatusIds = source.StatusIds == null ? new List<ContentId>() : new List<ContentId>(source.StatusIds),
                    StatusDurations = BoardSimulation.CloneStatusDurations(source.StatusDurations)
                };
            }
        }

        private sealed class MatchRun
        {
            public readonly ContentId GemId;
            public readonly bool IsHorizontal;
            public readonly List<GridCell> Cells;

            public MatchRun(ContentId gemId, bool isHorizontal, List<GridCell> cells)
            {
                GemId = gemId;
                IsHorizontal = isHorizontal;
                Cells = cells;
            }
        }

        private sealed class MatchGroup
        {
            private readonly HashSet<GridCell> _cells = new HashSet<GridCell>();
            private bool _hasHorizontal;
            private bool _hasVertical;
            private int _maximumRunLength;

            public readonly ContentId GemId;
            public List<GridCell> Cells { get; private set; }
            public bool CreatesPrism => _maximumRunLength >= 5 || (_hasHorizontal && _hasVertical);
            public bool CreatesMatchFourSpecial => !CreatesPrism && _maximumRunLength == 4;
            public string Pattern => CreatesPrism ? "prism" : CreatesMatchFourSpecial ? "match4" : "match3";

            public MatchGroup(ContentId gemId)
            {
                GemId = gemId;
                Cells = new List<GridCell>();
            }

            public bool Overlaps(MatchRun run)
            {
                if (!GemId.Equals(run.GemId)) return false;
                foreach (var cell in run.Cells)
                    if (_cells.Contains(cell)) return true;
                return false;
            }

            public void Add(MatchRun run)
            {
                foreach (var cell in run.Cells) _cells.Add(cell);
                if (run.IsHorizontal) _hasHorizontal = true;
                else _hasVertical = true;
                _maximumRunLength = Math.Max(_maximumRunLength, run.Cells.Count);
            }

            public void Merge(MatchGroup other)
            {
                foreach (var cell in other._cells) _cells.Add(cell);
                _hasHorizontal |= other._hasHorizontal;
                _hasVertical |= other._hasVertical;
                _maximumRunLength = Math.Max(_maximumRunLength, other._maximumRunLength);
            }

            public void FinalizeCells()
            {
                Cells = new List<GridCell>(_cells);
                Cells.Sort();
            }
        }

        private readonly struct SpecialCreation
        {
            public readonly GridCell Cell;
            public readonly ContentId GemId;
            public readonly ContentId SpecialId;
            public readonly ContentId SourceGemId;

            public SpecialCreation(GridCell cell, ContentId gemId, ContentId specialId, ContentId sourceGemId)
            {
                Cell = cell;
                GemId = gemId;
                SpecialId = specialId;
                SourceGemId = sourceGemId;
            }
        }

        private readonly struct GemMove
        {
            public readonly GridCell From;
            public readonly GridCell To;
            public readonly BoardGemState Gem;

            public GemMove(GridCell from, GridCell to, BoardGemState gem)
            {
                From = from;
                To = to;
                Gem = gem;
            }
        }
    }
}
