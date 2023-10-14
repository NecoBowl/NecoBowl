using neco_soft.NecoBowlDefinitions.Card;
using NecoBowl.Core;
using NecoBowl.Core.Input;
using NecoBowl.Core.Tactics;
using NecoBowl.Core.Tags;

var context = new NecoBowlContext(new());

void StuffA()
{
    context.SendInput(new NecoInput.PlaceCard(context.Players.Offense, new UnitCard(Chicken.Instance), (3, 3)));
    context.SendInput(new NecoInput.PlaceCard(context.Players.Offense, new UnitCard(Chicken.Instance), (3, 2)));
    context.SendInput(new NecoInput.PlaceCard(context.Players.Defense, new UnitCard(Chicken.Instance), (3, 5)));
    context.SendInput(new NecoInput.PlaceCard(context.Players.Defense, new UnitCard(Crab.Instance), (0, 0)));
    context.SendInput(new NecoInput.PlaceCard(context.Players.Defense, new UnitCard(Goose.Instance), (4, 0)));
    context.SendInput(new NecoInput.PlaceCard(context.Players.Offense, new UnitCard(Donkey.Instance), (1, 0)));
}

void StuffB()
{
    var boarCard = new UnitCard(Boar.Instance);
    context.SendInput(new NecoInput.PlaceCard(context.Players.Offense, boarCard, (1, 1)));
    context.SendInput(
        new NecoInput.SetPlanMod(
            context.Players.Offense,
            boarCard,
            CardOptionPermission.Rotate.StaticIdentifier,
            2));

    context.SendInput(new NecoInput.PlaceCard(context.Players.Offense, new UnitCard(Chicken.Instance), (2, 1)));
}

void Horse()
{
    var boarCard = new UnitCard(Donkey.Instance);
    context.SendInput(new NecoInput.PlaceCard(context.Players.Offense, boarCard, (2, 4)));
}

Horse();

context.FinishTurn();
var play = context.BeginPlay();

while (!play.IsFinished) {
    Console.ReadLine();
    play.Step();
}
