namespace SBCameraScroll;

public static class ShortcutHandlerMod {
    // ---------------- //
    // public functions //
    // ---------------- //

    public static ShortcutHandler.ShortCutVessel? GetShortcutVessel(ShortcutHandler? shortcut_handler, AbstractCreature? abstract_creature) {
        if (shortcut_handler == null || abstract_creature == null || abstract_creature.realizedCreature?.inShortcut == false) {
            return null;
        }


        foreach (AbstractPhysicalObject abstract_physical_object in abstract_creature.GetAllConnectedObjects()) // needed when carried by other creatures
        {
            if (abstract_physical_object.realizedObject is Creature creature) {
                foreach (ShortcutHandler.ShortCutVessel vessel in shortcut_handler.transportVessels) {
                    if (vessel.creature == creature) {
                        return vessel;
                    }
                }
            }
        }
        return null;
    }
}
