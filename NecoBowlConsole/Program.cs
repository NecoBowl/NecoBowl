using neco_soft.NecoBowlCore.Input;
using neco_soft.NecoBowlCore.Tactics;
using neco_soft.NecoBowlCore.Tags;
using neco_soft.NecoBowlDefinitions.Card;

var context = new NecoBowlContext(new());

void StuffA()
{
    context.SendInput(new NecoInput.PlaceCard(context.Players.Offense, new NecoUnitCard(Chicken.Instance), (3, 3)));
    context.SendInput(new NecoInput.PlaceCard(context.Players.Offense, new NecoUnitCard(Chicken.Instance), (3, 2)));
    context.SendInput(new NecoInput.PlaceCard(context.Players.Defense, new NecoUnitCard(Chicken.Instance), (3, 5)));
    context.SendInput(new NecoInput.PlaceCard(context.Players.Defense, new NecoUnitCard(Crab.Instance), (0, 0)));
    context.SendInput(new NecoInput.PlaceCard(context.Players.Defense, new NecoUnitCard(Goose.Instance), (4, 0)));
    context.SendInput(new NecoInput.PlaceCard(context.Players.Offense, new NecoUnitCard(Donkey.Instance), (1, 0)));
}

void StuffB()
{
    var boarCard = new NecoUnitCard(Boar.Instance);
    context.SendInput(new NecoInput.PlaceCard(context.Players.Offense, boarCard, (1, 1)));
    context.SendInput(new NecoInput.SetPlanMod(context.Players.Offense,
        boarCard,
        NecoCardOptionPermission.Rotate.StaticIdentifier,
        2));

    context.SendInput(new NecoInput.PlaceCard(context.Players.Offense, new NecoUnitCard(Chicken.Instance), (2, 1)));
}

void Horse()
{
    var boarCard = new NecoUnitCard(Donkey.Instance);
    context.SendInput(new NecoInput.PlaceCard(context.Players.Offense, boarCard, (2, 4)));
    context.SendInput(new NecoInput.SetPlanMod(context.Players.Offense,
        boarCard,
        nameof(NecoCardOptionPermission.FlipX),
        true));
}

Horse();

context.FinishTurn();
var play = context.BeginPlay();

while (!play.IsFinished) {
    Console.ReadLine();
    play.Step();
}
