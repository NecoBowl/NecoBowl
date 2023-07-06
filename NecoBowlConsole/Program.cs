using neco_soft.NecoBowlCore.Action;
using neco_soft.NecoBowlDefinitions.Unit;

const int FieldBounds = 5;
var field = new NecoField(FieldBounds, FieldBounds);
field[2, 2] = new NecoSpaceContents(new NecoUnit(Chicken.Instance, "A", new()));

var play = new NecoPlay(field);
    
while (play.IsFinished == false) {
    Console.WriteLine(play.Field.ToAscii());
    Console.ReadLine();
    
    play.Step();
}
