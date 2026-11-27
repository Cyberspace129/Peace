using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Chemicals", menuName = "Scriptable Objects/Chemicals")]
public class Chemicals : ScriptableObject
{
    // The make up of the Quark by it phases and other attarabutes
    public class flavor {
        private char Symbol;
        private int Generation;
        private char Charge_pollarity;
        private int Charge_top;
        private int Charge_bottom;
        private float Mass;
        private const float spin = 0.5;
    }

    List<flavor> Flavors =  new List<flavor>();
    Flavors.add( new flavor { Symbol='u', Generation=1, Charge_pollarity='p', Charge_top=2, Charge_bottom=3, Mass=2.2});
    Flavors.add( new flavor { Symbol='d', Generation=1, Charge_pollarity='n', Charge_top=1, Charge_bottom=3, Mass=4.7});
    Flavors.add( new flavor { Symbol='s', Generation=2, Charge_pollarity='p', Charge_top=2, Charge_bottom=3, Mass=1270.0});
    Flavors.add( new flavor { Symbol='c', Generation=2, Charge_pollarity='n', Charge_top=1, Charge_bottom=3, Mass=95.0});
    Flavors.add( new flavor { Symbol='t', Generation=3, Charge_pollarity='p', Charge_top=2, Charge_bottom=3, Mass=172700.0});
    Flavors.add( new flavor { Symbol='b', Generation=4, Charge_pollarity='n', Charge_top=1, Charge_bottom=3, Mass=4180.0});

    public class gluon {
        private List<flavor> hydron;        // one
        private List<flavor> baryons;       // two
        private List<flavor> Mesons;        // three
        private List<flavor> tetraquarks;   // four
        private List<flavor> pentaquarks;   // five
    }

    public class quark {
        
    }

    public class particle {
        
    }

    public class Atomic {
        // ========== private ==========
        private particle Electorns;
        private particle Neutrons;
        private particle Protons;


        // ========== public ==========
    }

    // === alpha particles, beta particles, gamma rays or neutrons ===
}
