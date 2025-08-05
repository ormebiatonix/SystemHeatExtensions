using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KSP.Localization;
using SystemHeat;

namespace SystemHeatExtensions
{
    class SHXConverter : ModuleSystemHeatConverter
    {
        // Heating based on Temperature. Overrides systemPower if active.
        [KSPField(isPersistant = false)]
        public FloatCurve heatingRate = new FloatCurve();

        // How many units of a discarded resource generates a KW of power, useful if
        // you want to make ThermalPower create waste heat when not utilized
        [KSPField(isPersistant = false)]
        private List<ResourceBaseRatio> thermalConversionResources = new List<ResourceBaseRatio>();

        // Overridden to allow for temperature-based heating
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.ModuleIsActive())
            {
                systemPower = heatingRate.Evaluate(heatModule.currentLoopTemperature);
            }
            else
            {
                systemPower = 0;
            }
        }

        // Overridden to create waste heat on discarded resources
        new protected void GenerateHeatFlight()
        {
            base.GenerateHeatFlight();
            if (base.ModuleIsActive())
            {
                float fluxScale = 1f;
                if (base.lastTimeFactor == 0d)
                {
                    fluxScale = 0f;
                }
                PartResourceList resources = part.Resources;
                foreach (ResourceBaseRatio thermalConversionResource in thermalConversionResources)
                {
                    PartResource resource = resources.Get(thermalConversionResource.ResourceName);
                    bool match(ResourceRatio r) => r.ResourceName == thermalConversionResource.ResourceName;
                    ResourceRatio outputResourceRatio = outputList.Find((Predicate<ResourceRatio>)match);
                    if (resource.maxAmount < resource.amount + 
                        (outputResourceRatio.Ratio * systemEfficiency.Evaluate(heatModule.currentLoopTemperature))
                        && (outputResourceRatio.DumpExcess == true))
                    {
                        heatModule.AddFlux(moduleID, systemOutletTemperature, (float)(fluxScale *
                                resource.amount + (outputResourceRatio.Ratio * systemEfficiency.Evaluate(
                                heatModule.currentLoopTemperature))), true);
                    }
                }
            }
        }
    }
}
