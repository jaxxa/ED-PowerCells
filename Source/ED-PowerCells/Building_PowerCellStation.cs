using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

using RimWorld;
using Verse;

namespace ED_PowerCells
{
    public class Building_PowerCellStation : Building
    {

        //UI elements
        private static Texture2D UI_CHARGED;
        private static Texture2D UI_DISCHARGED;

        CompPowerTrader m_Power;

        const int POWER_RATE_CHARGE = -2000;
        const int POWER_RATE_DISCHARGE = 2000;

        const int TICKS_TO_DISCHARGE = 10;

        const int TICKS_TO_CHARGE = 20;

        bool m_ChargeMode = true;

        Modes m_ErrorMode = Modes.Normal;

        int m_TicksRemaining = -1;


        public override void SpawnSetup()
        {
            base.SpawnSetup();

            UI_CHARGED = ContentFinder<Texture2D>.Get("Energy_Cell_Full", true);
            UI_DISCHARGED = ContentFinder<Texture2D>.Get("Energy_Cell_Empty", true);

            this.m_Power = base.GetComp<CompPowerTrader>();
        }

        public override void TickRare()
        {
            base.TickRare();

            //Charging
            if (this.m_ChargeMode)
            {
                this.m_Power.PowerOutput = Building_PowerCellStation.POWER_RATE_CHARGE;

                if (this.m_Power.PowerOn)
                {
                    this.m_TicksRemaining -= 1;

                    //Try Get a new cell if one is finished, ejecting the old one.
                    if (this.m_TicksRemaining <= 0)
                    {
                        if (this.TryGetPowerCell(false))
                        {
                            this.SpawnPowerCell(true);
                            this.m_TicksRemaining = Building_PowerCellStation.TICKS_TO_CHARGE;
                        }
                        else
                        {
                            this.m_ErrorMode = Modes.NoCell;
                        }
                    }
                    else
                    {
                        this.m_ErrorMode = Modes.Normal;
                    }
                }
                else
                {
                    this.m_ErrorMode = Modes.Low_Power;
                }
            }
            else //Discharging
            {
                this.m_TicksRemaining -= 1;
                if (this.m_TicksRemaining <= 0)
                {

                    if (this.TryGetPowerCell(true))
                    {
                        this.SpawnPowerCell(false);
                        this.m_TicksRemaining = Building_PowerCellStation.TICKS_TO_DISCHARGE;
                    }
                }

                if (this.m_TicksRemaining > 0)
                {
                    this.m_Power.PowerOutput = Building_PowerCellStation.POWER_RATE_DISCHARGE;
                    this.m_ErrorMode = Modes.Normal;
                }
                else
                {
                    this.m_Power.PowerOutput = 0;
                    this.m_ErrorMode = Modes.NoCell;
                }

            }

        }

        private void SpawnPowerCell(bool charged)
        {
            ThingDef _NewResourceDef;

            if (charged)
            {
                _NewResourceDef = ThingDef.Named("PowerCellCharged");
            }
            else
            {
                _NewResourceDef = ThingDef.Named("PowerCellDischarged");
            }

            if (_NewResourceDef == null) { return; }

            Thing _NewResource = ThingMaker.MakeThing(_NewResourceDef);
            GenPlace.TryPlaceThing(_NewResource, this.InteractionCell, ThingPlaceMode.Near);
        }

        #region UI

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();


            if (this.m_ChargeMode)
            {
                switch (this.m_ErrorMode)
                {
                    case Modes.Normal:

                        stringBuilder.AppendLine("Charing, remaining: " + this.m_TicksRemaining.ToString());
                        break;
                    case Modes.Low_Power:
                        stringBuilder.AppendLine("Charing - Low Power.");
                        break;

                    case Modes.NoCell:
                        stringBuilder.AppendLine("Charing - No Uncharged Cell.");
                        break;
                }
            }
            else
            {
                switch (this.m_ErrorMode)
                {
                    case Modes.Normal:

                        stringBuilder.AppendLine("Discharing, remaining: " + this.m_TicksRemaining.ToString());
                        break;
                    case Modes.Low_Power:
                        stringBuilder.AppendLine("Discharing - Low Power.");
                        break;

                    case Modes.NoCell:
                        stringBuilder.AppendLine("Discharing - No Uncharged Cell.");
                        break;
                }
            }

            if (m_Power != null)
            {
                string text = m_Power.CompInspectStringExtra();
                if (!text.NullOrEmpty())
                {
                    stringBuilder.AppendLine(text);
                }
            }

            return stringBuilder.ToString();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            //Add the stock Gizmoes
            foreach (var g in base.GetGizmos())
            {
                yield return g;
            }

            if (true)
            {
                if (this.m_ChargeMode)
                {

                    Command_Action act = new Command_Action();
                    //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                    act.action = () => this.SwitchChargeDischarge();
                    act.icon = UI_CHARGED;
                    act.defaultLabel = "Charging";
                    act.defaultDesc = "Charging";
                    act.activateSound = SoundDef.Named("Click");
                    //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                    //act.groupKey = 689736;
                    yield return act;
                }
                else
                {

                    Command_Action act = new Command_Action();
                    //act.action = () => Designator_Deconstruct.DesignateDeconstruct(this);
                    act.action = () => this.SwitchChargeDischarge();
                    act.icon = UI_DISCHARGED;
                    act.defaultLabel = "Discharging";
                    act.defaultDesc = "Discharging";
                    act.activateSound = SoundDef.Named("Click");
                    //act.hotKey = KeyBindingDefOf.DesignatorDeconstruct;
                    //act.groupKey = 689736;
                    yield return act;
                }
            }
        }

        private void SwitchChargeDischarge()
        {
            this.m_ChargeMode = !this.m_ChargeMode;
        }

        #endregion

        private bool TryGetPowerCell(bool charged)
        {
            List<IntVec3> _Cells = Enumerable.ToList<IntVec3>(Enumerable.Where<IntVec3>(GenAdj.CellsAdjacentCardinal((Thing)this), (Func<IntVec3, bool>)(c => GenGrid.InBounds(c))));

            List<Thing> _CloseThings = new List<Thing>();

            foreach (IntVec3 _Cell in _Cells)
            {
                _CloseThings.AddRange(GridsUtility.GetThingList(_Cell));
            }

            foreach (Thing _CloseThing in _CloseThings)
            {
                Log.Message(_CloseThing.def.defName);
                if (charged)
                {

                    if (string.Equals(_CloseThing.def.defName, "PowerCellCharged"))
                    {
                        _CloseThing.SplitOff(1);
                        return true;
                    }
                }
                else
                {
                    if (string.Equals(_CloseThing.def.defName, "PowerCellDischarged"))
                    {
                        _CloseThing.SplitOff(1);
                        return true;
                    }
                }
            }

            return false;
        }


        #region Saving

        //Saving game
        public override void ExposeData()
        {
            base.ExposeData();

            //  Scribe_Deep.LookDeep(ref shieldField, "shieldField");

            Scribe_Values.LookValue(ref m_ChargeMode, "m_ChargeMode");
            Scribe_Values.LookValue(ref m_TicksRemaining, "m_TicksRemaining");

        }

        #endregion

    }


    enum Modes { Normal, Low_Power, NoCell };
}



//private List<Thing> FindValidStuffNearBuilding(Thing centerBuilding, int radius)
//{

//    //IEnumerable<Thing> _closeThings = GenRadial.RadialDistinctThingsAround(centerBuilding.Position, radius, true);

//    List<Thing> _closeThings = new List<Thing>();

//    List<IntVec3> _Cells = Enumerable.ToList<IntVec3>(Enumerable.Where<IntVec3>(GenAdj.CellsAdjacentCardinal((Thing)this), (Func<IntVec3, bool>)(c => GenGrid.InBounds(c))));

//    foreach (IntVec3 _Cell in _Cells)
//    {
//        _closeThings.AddRange(GridsUtility.GetThingList(_Cell));
//    }

//    List<Thing> _ValidCloseThings = new List<Thing>();

//    foreach (Thing _TempThing in _closeThings)
//    {
//        if (_TempThing.stackCount > Building_MolecularReinforcmentCompressor.STUFF_AMMOUNT_REQUIRED)
//        {
//            ThingDef _ReinforcedVersion = this.GetReinforcedVersion(_TempThing);

//            if (_ReinforcedVersion != null)
//            {
//                _ValidCloseThings.Add(_TempThing);
//                _TempThing.stackCount -= Building_MolecularReinforcmentCompressor.STUFF_AMMOUNT_REQUIRED;
//            }
//        }
//        //if (tempThing.def.category == ThingCategory.Item)
//        //{
//        //    validCloseThings.Add(tempThing);
//        //}
//    }
//    return _ValidCloseThings;
//}