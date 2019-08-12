// CivOne
//
// To the extent possible under law, the person who associated CC0 with
// CivOne has waived all copyright and related or neighboring rights
// to CivOne.
//
// You should have received a copy of the CC0 legalcode along with this
// work. If not, see <http://creativecommons.org/publicdomain/zero/1.0/>.

using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CivOne.Advances;
using CivOne.Buildings;
using CivOne.Enums;
using CivOne.Leaders;
using CivOne.Tasks;
using CivOne.Tiles;
using CivOne.Units;

// TODO fire-eggs consider replacing any scan of all units [GetUnits] with a subset of units [All units immediately surrounding x,y]

namespace CivOne
{
    // ReSharper disable once InconsistentNaming
    internal partial class AI : BaseInstance
	{
        private Player Player { get; }
        private ILeader Leader => Player.Civilization.Leader;

        private static int Distance(City city, IUnit unit)
        {
            return Common.Distance(city.X, city.Y, unit.X, unit.Y);
        }
        private static int Distance(IUnit unit1, IUnit unit2)
        {
            return Common.Distance(unit1.X, unit1.Y, unit2.X, unit2.Y);
        }

		internal void Move(IUnit unit)
		{
			if (Player != unit.Owner) 
                return;

			if (unit.Owner == 0)
			{
				BarbarianMove(unit);
				return;
			}

            switch (unit.Role)
            {
                case UnitRole.Settler:
                    SettlerMove(unit);
                    break;
                case UnitRole.Defense:
                    if (!DefenseMove(unit))
                        LandAttackMove(unit);
                    break;
                case UnitRole.LandAttack:
                    LandAttackMove(unit);
                    break;
                case UnitRole.SeaAttack:
                case UnitRole.AirAttack:
                case UnitRole.Transport:
                case UnitRole.Civilian:
                    Game.DisbandUnit(unit);
                    break;
            }

		}

        private enum Improvement
        {
            None = 0,
            Irrigate,
            Mine,
            Already
        }

        /* checks a square for potential improvements and returns:
         - 0 if the square is already improved or has a city
         - 2 if mining the square can provide at least 2 shields
         - 1 if irrigating the sqaure can provide at 1 food and the sqaure is next to an ocean or river square
         - 0 otherwise

            TODO: fire-eggs should take specials into account?
            TODO: fire-eggs should take government into account?
            TODO: fire-eggs tile _conversion_ to be taken into account? (e.g. swamp->grasslands)
        */
        private Improvement CheckPossibleTerrainImprovementBonus(ITile tile)
        {
            if (tile.HasCity || tile.Irrigation || tile.Mine)
                return Improvement.Already;

            // TODO fire-eggs: SWY's "MiningShieldBonus" doesn't work here, need to understand it
            if (tile.Type == Terrain.Hills)
                return Improvement.Mine;

            bool usefulIrrigation = (tile is Grassland ||
                                     tile.Type == Terrain.Plains ||
                                     tile.Type == Terrain.Desert) && 
                                    tile.AllowIrrigation();

            // TODO fire-eggs: SWY's "IrrigationFoodBonus" doesn't work here, need to understand it
            if (usefulIrrigation)
                return Improvement.Irrigate;
            return Improvement.None;
        }

        private void SettlerMove(IUnit unit)
        {
            ITile tile = unit.Tile;

            // seg010_13CB
            int bestLandValue = tile.LandValue;
            if (bestLandValue != 0)
            {
                // TODO seg010_13F6

                // at beginning of game, compelled to build city ASAP
                if (Game.GameTurn == 0) // && !Game.IsEarth // TODO not playing on EARTH
                {
                    GameTask.Enqueue(Orders.FoundCity(unit as Settlers));
                    return;
                }

                // TODO seg010_14B6
            }

            bool noCity = !tile.HasCity;
            //bool validCity = (tile is Grassland || tile is River || tile is Plains) && !tile.HasCity;
            //bool validIrrigation = (tile is Grassland || tile is River || tile is Plains || tile is Desert) && noCity && (!tile.Mine) && (!tile.Irrigation) && tile.CrossTiles().Any(x => x.IsOcean || x is River || x.Irrigation);
            //bool validMine = (tile is Mountains || tile is Hills) && noCity && (!tile.Mine) && (!tile.Irrigation);
            //bool validRoad = noCity && !tile.Road;

            City nearCity = FindNearestCity(unit.X, unit.Y, out var distNearCity);

            // seg010_1513
            if (Game.Difficulty != 0 &&
                nearCity != null && 
                Human == nearCity.Owner && // SEG010_1526: closest city belongs to human?
                !IsEnemyUnitNearby(unit) &&
                distNearCity > 1 &&
                nearCity.Owner != unit.Owner && // seg010_1550: nearest city belongs to other civ
                // TODO seg010_1558: our techcount is less than humans?
                tile.LandValue >= 9 &&
                (14 - distNearCity) <= tile.LandValue &&
                noCity) // TODO fire-eggs: by definition true because distNearCity != 0 ?
            {
                GameTask.Enqueue(Orders.FoundCity(unit as Settlers));
                return;
            }

            // TODO fire-eggs does seg010_2192 (plunder check) apply?

            // TODO fire-eggs seg010_221B: check not in ocean [on ship?]

            // TODO seg010_2263: something about civ able to improve before MONARCHY (?)
            // TODO seg010_227A: AIContinentPolicy
            if (distNearCity > 0 &&                // seg010_2283
                distNearCity <= 2 &&               // seg010_228C
                nearCity != null && 
                nearCity.Owner == unit.Owner &&    // seg010_2295
                (nearCity.Size >= 3 ||             // seg010_22AA
                 tile.Type != Terrain.Hills ||     // seg010_22B4
                 tile.Special))                    // seg010_22BD
            {
                // seg010_22E0: do the best improvement if possible: irrigate or mine
                switch (CheckPossibleTerrainImprovementBonus(tile))
                {
                    case Improvement.Irrigate:
                        GameTask.Enqueue(Orders.BuildIrrigation(unit));
                        return;
                    case Improvement.Mine:
                        GameTask.Enqueue(Orders.BuildMines(unit));
                        return;
                }

                // seg010_233D:
                if ((tile.Mine || tile.Irrigation) &&
                    !tile.Road &&
                    (tile.Type == Terrain.Desert || 
                     tile.Type == Terrain.Plains || 
                     tile.Type == Terrain.Grassland1 ||
                     tile.Type == Terrain.Grassland2))
                {
                    GameTask.Enqueue(Orders.BuildRoad(unit));
                    return;
                }

                if (Player.HasAdvance<RailRoad>() &&
                    !tile.RailRoad)
                {
                    // TODO seg010_23A7 decide whether to build railroad
                }
            }

            // TODO seg010_23EF: clean up pollution

            // TODO seg010_240A: do nothing based on civ expansionist attitude?

            // TODO seg010_245C: logic when next-to or in own city

            // TODO seg010_25F3

            // TODO seg010_2735

            // TODO seg010_2755

            //if (validCity && nearestCity > 3)
            //{
            //    GameTask.Enqueue(Orders.FoundCity(unit as Settlers));
            //    return;
            //}

            //if (nearestOwnCity < 3)
            //{
            //    switch (Common.Random.Next(5 * nearestOwnCity))
            //    {
            //        case 0:
            //            if (validRoad)
            //            {
            //                GameTask.Enqueue(Orders.BuildRoad(unit));
            //                return;
            //            }
            //            break;
            //        case 1:
            //            if (validIrrigation)
            //            {
            //                Debug.Assert(!(tile is Mountains));
            //                GameTask.Enqueue(Orders.BuildIrrigation(unit));
            //                return;
            //            }
            //            break;
            //        case 2:
            //            if (validMine)
            //            {
            //                GameTask.Enqueue(Orders.BuildMines(unit));
            //                return;
            //            }
            //            break;
            //    }
            //}

            // random move from SWY
            for (int i = 0; i < 1000; i++)
            {
                int relX = Common.Random.Next(-1, 2);
                int relY = Common.Random.Next(-1, 2);
                if (relX == 0 && relY == 0) 
                    continue;
                if (unit.Tile[relX, relY].Type == Terrain.Ocean) 
                    continue;
                if (unit.Tile[relX, relY].Units.Any(x => x.Owner != unit.Owner)) 
                    continue;
                if (!unit.MoveTo(relX, relY)) 
                    continue;
                return;
            }
            unit.SkipTurn();

        }

        private bool isBestDefenseInLocation(IUnit unit)
        {
            // Does this unit have the best defense value at it's current location?
            // TODO fire-eggs implement!
            return false;
        }

        private bool DefenseMove(IUnit unit)
        {
            // NOTE: all fortified units appear to be woken up periodically:
            // see seg010_177A, seg010_17A1

            // 1. In city?
            if (unit.Tile.City != null)
            {
                // 1a. alone in city: fortify [seg010_15CB]
                // TODO fire-eggs: to match SWY's production logic, target is *2* units in the city
                // 1b. best defense unit in city: fortify [seg010_15EB]
                if (unit.Tile.Units.Length <= 2 || 
                    isBestDefenseInLocation(unit))
                {
                    unit.Fortify = true;
                    return true;
                }

                // TODO seg010_1655 to seg010_170A
                // 1c. [determine nearby threat: fortify or assignNewTacticalLocation];
                //     does this mean to wake up other units?
            }

            // TODO let the LandAttackMove() call do this instead?
            // 2. pillage check: unit _type_ has < 2 moves
            City nearCity = FindNearestCity(unit.X, unit.Y, out int distNearCity);
            if (PillageCheck(unit, distNearCity, nearCity?.Owner ?? 0))
                return true;

            // 3. seg010_2899: fortify under specific situation

            // 4. treat as attack unit
            return false;

            //unit.Fortify = true;
            //while (unit.Tile.City != null && 
            //       unit.Tile.Units.Count(x => x.Role == UnitRole.Defense) > 2)
            //{
            //    IUnit disband;
            //    IUnit[] units = unit.Tile.Units.Where(x => x != unit).ToArray();
            //    if ((disband = unit.Tile.Units.FirstOrDefault(x => x is Militia)) != null) 
            //    { Game.DisbandUnit(disband); continue; }
            //    if ((disband = unit.Tile.Units.FirstOrDefault(x => x is Phalanx)) != null) 
            //    { Game.DisbandUnit(disband); continue; }
            //    if ((disband = unit.Tile.Units.FirstOrDefault(x => x is Musketeers)) != null) 
            //    { Game.DisbandUnit(disband); continue; }
            //    if ((disband = unit.Tile.Units.FirstOrDefault(x => x is Riflemen)) != null) 
            //    { Game.DisbandUnit(disband); continue; }
            //    if ((disband = unit.Tile.Units.FirstOrDefault(x => x is MechInf)) != null) 
            //    { Game.DisbandUnit(disband); continue; }
            //}
        }

        private bool PillageCheck(IUnit unit, int distNearCity, byte nearCityOwner)
        {
            // DarkPanda ai_orders seg010_2192

            if (unit.Move < 2 &&
                distNearCity < 4 &&
                Human == nearCityOwner &&
                // at war with human && // TODO diplomacy
                (unit.Tile.Irrigation || unit.Tile.Mine))
            {
                // pillage the square
                unit.Pillage();
                return true;
            }

            return false;
        }

        private void LandAttackMove(IUnit unit)
        {
            // TODO seg010_13CB
            // if (bestLandValue != 0)
            //     seg010_1461: check for tribal hut
            // seg010_14b6: setStrategicLocation

            // TODO seg010_15AA / seg010_17A7
            // if (unit in city)
            //   distToClosestUnit > 0
            //      assignNewTacticalLocation

            bool isEnemyNearby = IsEnemyUnitNearby(unit);
            City nearCity = FindNearestCity(unit.X, unit.Y, out int distNearCity);

            // DarkPanda ai_orders seg010_2192
            if (PillageCheck(unit, distNearCity, nearCity?.Owner ?? 0))
                return;

            // DarkPanda ai_orders seg010_2857
            if (!unit.Goto.IsEmpty &&
                !isEnemyNearby)
            {
                return; //continue with goto unless enemy encountered
            }

            // seg010_2980 === var_5C
            bool enemyUnitOrCityNearby = IsEnemyUnitOrCityNearby(unit);

            // TODO seg010_29C5

            // seg010_29E7: neighbor loop
            int bestValue = int.MinValue;
            ITile bestNeighbor = null;

            foreach (var tile in unit.Tile.GetBorderTiles())
            {
                if (tile.IsOcean)
                    continue; // ignore ocean tiles

                int neighOwner = getOwner(tile);
                var neighUnits = tile.Units;
                bool neighOwnUnits = neighUnits.Any(x => x.Owner == unit.Owner);
                bool neighEnemyUnits = neighUnits.Any(x => x.Owner != unit.Owner); // var_1C equivalent?


                // TODO seg010_2A7B: diplomat logic

                // TODO seg010_2B0D: skip this neighbor if too many enemies near?

                // TODO seg010_2C06: skip this neighbor if cannot stack?

                // TODO seg010_2C6D: visibility_flag case - NYI

                // seg010_2CE1
                int neighborValue = Common.Random.Next(5);
                if (neighOwnUnits)
                    neighborValue += aggregateUnitStackAttribute(neighUnits[0], 3) * 2 /
                                     (aggregateUnitStackAttribute(neighUnits[0], 1) + 1);
                else
                {
                    neighborValue += tile.Defense * 4;
                }

                // TODO seg010_2DF3 : AImilitaryPower

                // seg010_2E43
                if (neighEnemyUnits)
                {
                    // TODO seg010_2E6D: attempt to bribe unit under specific conditions

                }

                // seg010_31A5: neighbor unit(s) are our own
                if (neighOwnUnits)
                {
                    neighborValue -= unit.Defense;
                }

                if (neighUnits.Length < 1)
                {
                    // TODO state of war / diplomacy
                    // seg010_31C7: undefended enemy city, lets attack
                    var neighCity = tile.City;
                    if (neighCity != null && neighCity.Owner != unit.Owner)
                        neighborValue = int.MaxValue;

                    // seg010_31EC: a hut is desirable
                    if (tile.Hut)
                        neighborValue += 20;
                }

                // seg010_3209
                if (!enemyUnitOrCityNearby)
                {
                    neighborValue += EvaluateNextTileOut(unit, tile);
                }

                if (neighborValue > bestValue)
                {
                    bestValue = neighborValue;
                    bestNeighbor = tile;
                }

            } // neighbor eval loop

            int deltaX = 0;
            int deltaY = 0;
            if (bestNeighbor.X < unit.X)
                deltaX = -1;
            else if (bestNeighbor.X > unit.X)
                deltaX = +1;
            if (bestNeighbor.Y < unit.Y)
                deltaY = -1;
            else if (bestNeighbor.Y > unit.Y)
                deltaY = +1;

            if (!unit.MoveTo(deltaX, deltaY))
            {
                unit.MovesLeft = 0; // TODO fire-eggs failed to move; make sure we don't try to move again; infinite loop
            }

            //RandomMove(unit);
        }

        // There were no enemies or cities nearby. Look to the neighbors of the neighbor tile,
        // and return the DELTA impact on the value of the neighbor.
        private int EvaluateNextTileOut(IUnit unit, ITile aTile)
        {
            int neighborValueDelta = 0;

            // TODO seg010_3212 : don't know what the purpose of seg029_1498[] is

            // seg010_329D : determine if the direction we're going is toward ocean or other units
            foreach (var tile in aTile.GetBorderTiles())
            {
                // TODO validate within map range
                if (tile == null)
                    continue;

                // TODO visibility seg010_32EC

                if (CanSee(unit, tile))
                {
                    if (!tile.IsOcean)
                        neighborValueDelta += 2;
                }

                if (tile.Units.Length > 0)
                    neighborValueDelta -= 2;
            }

            return neighborValueDelta;
        }

        private bool CanSee(IUnit unit, ITile atile)
        {
            // TODO is this 'explored' or 'currently visible'?
            // has this Civ seen a particular tile?
            // MapVisibility[x,y] for unit.Civ == 1
            return true;
        }
        private void RandomMove(IUnit unit)
        {
            for (int i = 0; i < 1000; i++)
            {
                if (unit.Goto.IsEmpty)
                {
                    int gotoX = Common.Random.Next(-5, 6);
                    int gotoY = Common.Random.Next(-5, 6);
                    if (gotoX == 0 && gotoY == 0) continue;
                    if (!Player.Visible(unit.X + gotoX, unit.Y + gotoY)) continue;

                    unit.Goto = new Point(unit.X + gotoX, unit.Y + gotoY);
                    continue;
                }

                if (!unit.Goto.IsEmpty)
                {
                    int distance = unit.Tile.DistanceTo(unit.Goto);
                    ITile[] tiles = unit.MoveTargets.OrderBy(x => x.DistanceTo(unit.Goto)).ThenBy(x => x.Movement).ToArray();
                    if (tiles.Length == 0 || tiles[0].DistanceTo(unit.Goto) > distance)
                    {
                        // No valid tile to move to, cancel goto
                        unit.Goto = Point.Empty;
                        continue;
                    }
                    else if (tiles[0].DistanceTo(unit.Goto) == distance)
                    {
                        // Distance is unchanged, 50% chance to cancel goto
                        if (Common.Random.Next(0, 100) < 50)
                        {
                            unit.Goto = Point.Empty;
                            continue;
                        }
                    }

                    if (tiles[0].Units.Any(x => x.Owner != unit.Owner))
                    {
                        if (unit.Role == UnitRole.Civilian || unit.Role == UnitRole.Settler)
                        {
                            // do not attack with civilian or settler units
                            unit.Goto = Point.Empty;
                            continue;
                        }

                        if (unit.Role == UnitRole.Transport && Common.Random.Next(0, 100) < 67)
                        {
                            // 67% chance of cancelling attack with transport unit
                            unit.Goto = Point.Empty;
                            continue;
                        }

                        if (unit.Attack < tiles[0].Units.Select(x => x.Defense).Max() && Common.Random.Next(0, 100) < 50)
                        {
                            // 50% of attacking cancelling attack of stronger unit
                            unit.Goto = Point.Empty;
                            continue;
                        }
                    }

                    if (!unit.MoveTo(tiles[0].X - unit.X, tiles[0].Y - unit.Y))
                    {
                        // The code below is to prevent the game from becoming stuck...
                        if (Common.Random.Next(0, 100) < 67)
                        {
                            unit.Goto = Point.Empty;
                            continue;
                        }
                        else if (Common.Random.Next(0, 100) < 67)
                        {
                            unit.SkipTurn();
                            return;
                        }
                        else
                        {
                            Game.DisbandUnit(unit);
                            return;
                        }
                    }

                    return;
                }
            }

            unit.SkipTurn();
            return;
        }

        private int aggregateUnitStackAttribute(IUnit unit, int p1)
        {
            // TODO NYI
            return 1;
        }

        private int getOwner(ITile tile)
        {
            // TODO owner NYI
            return 0;
        }

        internal void ChooseResearch()
		{
			if (Player.CurrentResearch != null) return;
			
			IAdvance[] advances = Player.AvailableResearch.ToArray();
			
			// No further research possible
			if (advances.Length == 0) return;

			Player.CurrentResearch = advances[Common.Random.Next(0, advances.Length)];

			Log($"AI: {Player.LeaderName} of the {Player.TribeNamePlural} starts researching {Player.CurrentResearch.Name}.");
		}

		internal void CityProduction(City city)
		{
			if (city == null || city.Size == 0 || city.Tile == null || Player != city.Owner) return;

			IProduction production = null;

			// Create 2 defensive units per city
			if (Player.HasAdvance<LaborUnion>())
			{
				if (city.Tile.Units.Count(x => x.Type == UnitType.MechInf) < 2) production = new MechInf();
			}
			else if (Player.HasAdvance<Conscription>())
			{
				if (city.Tile.Units.Count(x => x.Type == UnitType.Riflemen) < 2) production = new Riflemen();
			}
			else if (Player.HasAdvance<Gunpowder>())
			{
				if (city.Tile.Units.Count(x => x.Type == UnitType.Musketeers) < 2) production = new Musketeers();
			}
			else if (Player.HasAdvance<BronzeWorking>())
			{
				if (city.Tile.Units.Count(x => x.Type == UnitType.Phalanx) < 2) production = new Phalanx();
			}
			else
			{
				if (city.Tile.Units.Count(x => x.Type == UnitType.Militia) < 2) production = new Militia();
			}
			
			// Create city improvements
			if (production == null)
			{
				if (!city.HasBuilding<Barracks>()) 
                    production = new Barracks();
				else if (Player.HasAdvance<Pottery>() && !city.HasBuilding<Granary>()) 
                    production = new Granary();
				else if (Player.HasAdvance<CeremonialBurial>() && !city.HasBuilding<Temple>()) 
                    production = new Temple();
				else if (Player.HasAdvance<Masonry>() && !city.HasBuilding<CityWalls>()) 
                    production = new CityWalls();
			}

			// Create Settlers
			if (production == null)
			{
				int minCitySize = Leader.Development == DevelopmentLevel.Expansionistic ? 2 : Leader.Development == DevelopmentLevel.Normal ? 3 : 4;
				int maxCities = Leader.Development == DevelopmentLevel.Expansionistic ? 13 : Leader.Development == DevelopmentLevel.Normal ? 10 : 7;
				if (city.Size >= minCitySize && 
                    !city.Units.Any(x => x is Settlers) && 
                    Player.Cities.Length < maxCities) 
                    production = new Settlers();
			}

			// Create some other unit
			if (production == null)
			{
				if (city.Units.Length < 4)
				{
					if (Player.Government is Governments.Republic || Player.Government is Governments.Democracy)
					{
						if (Player.HasAdvance<Writing>()) production = new Diplomat();
					}
					else 
					{
						if (Player.HasAdvance<Automobile>()) production = new Armor();
						else if (Player.HasAdvance<Metallurgy>()) production = new Cannon();
						else if (Player.HasAdvance<Chivalry>()) production = new Knights();
						else if (Player.HasAdvance<TheWheel>()) production = new Chariot();
						else if (Player.HasAdvance<HorsebackRiding>()) production = new Cavalry();
						else if (Player.HasAdvance<IronWorking>()) production = new Legion();
					}
				}
				else
				{
					if (Player.HasAdvance<Trade>()) production = new Caravan();
				}
			}

			// Set random production
			if (production == null)
			{
				IProduction[] items = city.AvailableProduction.ToArray();
				production = items[Common.Random.Next(items.Length)];
			}

			city.SetProduction(production);
		}

		private static readonly Dictionary<Player, AI> _instances = new Dictionary<Player, AI>();
		internal static AI Instance(Player player)
		{
			if (_instances.ContainsKey(player))
				return _instances[player];
			_instances.Add(player, new AI(player));
			return _instances[player];
		}

        // Adapted from darkpanda's civlogic port
        private static City FindNearestCity(int x, int y, out int distance)
        {
            City nearestCity = null;
            int bestDistance = int.MaxValue;
            foreach (var city in Game.GetCities())
            {
                var dist = Common.Distance(city.X, city.Y, x, y);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    nearestCity = city;
                }
            }

            distance = bestDistance;
            return nearestCity;
        }

        private static bool IsEnemyUnitOrCityNearby(IUnit unit)
        {
            bool isEnemyUnit = IsEnemyUnitNearby(unit);
            var city = FindNearestCity(unit.X, unit.Y,out int junk);
            if (city == null) 
                return isEnemyUnit;
            if (city.Owner != unit.Owner &&
                Distance(city, unit) == 1)
                return true;
            return isEnemyUnit;
        }

        private static bool IsEnemyUnitNearby(IUnit unit)
        {
            // TODO fire-eggs it is not specified what "nearby" means: assuming distance 1 for now
            int minX = unit.X - 1;
            int maxX = unit.X + 1;
            int minY = unit.Y - 1;
            int maxY = unit.Y + 1;
            var enemies = Game.GetUnits().Where(u => u.X >= minX &&
                                                     u.X <= maxX &&
                                                     u.Y >= minY &&
                                                     u.Y <= maxY &&
                                                     u.Owner != unit.Owner).ToArray();
            // TODO .Where(u=>Distance(unit,u) == 1 && u.Owner != unit.Owner)
            return enemies.Length > 0;
        }

        private AI(Player player)
		{
			Player = player;
		}
	}
}