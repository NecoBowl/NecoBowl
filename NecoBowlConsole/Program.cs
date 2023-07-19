using neco_soft.NecoBowlCore;
using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlCore.Model;
using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlDefinitions.Unit;

const int FieldBounds = 5;
var field = new NecoField(FieldBounds, FieldBounds);

var player1 = new NecoPlayer();
var player2 = new NecoPlayer();

var bigUnit = new NecoUnit(NecoUnitModelCustom.DoNothing("DoNothing_Strong", 5), player1.Id);
var smallUnit1 = new NecoUnit(NecoUnitModelCustom.Mover("SouthMover_Weak", 1, AbsoluteDirection.South),
    player2.Id);
var smallUnit2 = new NecoUnit(NecoUnitModelCustom.Mover("WestMover_Weak", 1, AbsoluteDirection.West),
    player2.Id);

field[0, 1] = new(bigUnit);
field[0, 2] = new(smallUnit1);
field[1, 1] = new(smallUnit2);

var play = new NecoPlay(field);
play.Step();
