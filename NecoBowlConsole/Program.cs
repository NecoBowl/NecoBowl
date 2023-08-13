using neco_soft.NecoBowlCore.Input;
using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlDefinitions.Card;

var context = new NecoBowlContext(new());
context.SendInput(new NecoInput.PlaceCard(context.Players.Offense, new NecoUnitCard(Dog.Instance), (3, 3)));

context.FinishTurn();
var play = context.BeginPlay();
play.Step();