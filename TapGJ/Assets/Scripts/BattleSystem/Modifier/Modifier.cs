public class Modifier
{
    public uint Id {  get; private set; }
    public E_ModifierType ModifierType { get; private set; }
    public float ModifierValue { get; private set; }

    private static uint GobalId = 0;

    public Modifier(E_ModifierType modifierType, float modifierValue)
    {
        Id = GobalId++;
        ModifierType = modifierType;
        ModifierValue = modifierValue;
    }
}