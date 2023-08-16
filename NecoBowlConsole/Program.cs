using neco_soft.NecoBowlCore.Input;
using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlDefinitions.Card;

var context = new NecoBowlContext(new());
context.SendInput(new NecoInput.PlaceCard(context.Players.Offense, new NecoUnitCard(Chicken.Instance), (3, 3)));
context.SendInput(new NecoInput.PlaceCard(context.Players.Offense, new NecoUnitCard(Chicken.Instance), (3, 2)));
context.SendInput(new NecoInput.PlaceCard(context.Players.Defense, new NecoUnitCard(Chicken.Instance), (3, 5)));

context.FinishTurn();
var play = context.BeginPlay();
play.Step();
