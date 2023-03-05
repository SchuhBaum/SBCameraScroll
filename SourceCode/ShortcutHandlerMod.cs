namespace SBCameraScroll;

public static class ShortcutHandlerMod
{
    // ---------------- //
    // public functions //
    // ---------------- //

    public static ShortcutHandler.ShortCutVessel? GetShortcutVessel(ShortcutHandler? shortcutHandler, AbstractCreature? abstractCreature)
    {
        if (shortcutHandler == null || abstractCreature == null || abstractCreature.realizedCreature?.inShortcut == false)
        {
            return null;
        }


        foreach (AbstractPhysicalObject abstractPhysicalObject in abstractCreature.GetAllConnectedObjects()) // needed when carried by other creatures
        {
            if (abstractPhysicalObject.realizedObject is Creature creature)
            {
                foreach (ShortcutHandler.ShortCutVessel vessel in shortcutHandler.transportVessels)
                {
                    if (vessel.creature == creature)
                    {
                        return vessel;
                    }
                }
            }
        }
        return null;
    }
}