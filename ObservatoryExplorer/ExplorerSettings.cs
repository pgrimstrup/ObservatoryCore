using Observatory.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Observatory.Explorer
{
    public class ExplorerSettings
    {
        public ExplorerSettings()
        {
            CustomCriteriaFile = $"{Environment.GetFolderPath(Environment.SpecialFolder.Personal)}{System.IO.Path.DirectorySeparatorChar}ObservatoryCriteria.lua";
        }

        [SettingDisplayNameAttribute("Landable & Terraformable")]
        public bool LandableTerraformable { get; set; }

        [SettingDisplayNameAttribute("Landable w/ Atmosphere")]
        public bool LandableAtmosphere { get; set; }

        [SettingDisplayNameAttribute("Landable High-g")]
        public bool LandableHighG { get; set; }

        [SettingDisplayNameAttribute("Landable Large")]
        public bool LandableLarge { get; set; }

        [SettingDisplayNameAttribute("Close Orbit")]
        public bool CloseOrbit { get; set; }

        [SettingDisplayNameAttribute("Shepherd Moon")]
        public bool ShepherdMoon { get; set; }

        [SettingDisplayNameAttribute("Wide Ring")]
        public bool WideRing { get; set; }

        [SettingDisplayNameAttribute("Close Binary")]
        public bool CloseBinary { get; set; }

        [SettingDisplayNameAttribute("Colliding Binary")]
        public bool CollidingBinary { get; set; }

        [SettingDisplayNameAttribute("Close Ring Proximity")]
        public bool CloseRing { get; set; }

        [SettingDisplayNameAttribute("Codex Discoveries")]
        public bool Codex { get; set; }

        [SettingDisplayNameAttribute("Uncommon Secondary Star")]
        public bool UncommonSecondary { get; set; }

        [SettingDisplayNameAttribute("Landable w/ Ring")]
        public bool LandableRing { get; set; }

        [SettingDisplayNameAttribute("Nested Moon")]
        public bool Nested { get; set; }

        [SettingDisplayNameAttribute("Small Object")]
        public bool SmallObject { get; set; }

        [SettingDisplayNameAttribute("Fast Rotation")]
        public bool FastRotation { get; set; }

        [SettingDisplayNameAttribute("Fast Orbit")]
        public bool FastOrbit { get; set; }

        [SettingDisplayNameAttribute("High Eccentricity")]
        public bool HighEccentricity { get; set; }

        [SettingDisplayNameAttribute("Diverse Life")]
        public bool DiverseLife { get; set; }

        [SettingDisplayNameAttribute("Good FSD Injection")]
        public bool GoodFSDBody { get; set; }

        [SettingDisplayNameAttribute("All FSD Mats In System")]
        public bool GreenSystem { get; set; }

        [SettingDisplayNameAttribute("All Surface Mats In System")]
        public bool GoldSystem { get; set; }

        [SettingDisplayNameAttribute("High-Value Body")]
        public bool HighValueMappable { get; set; }

        [SettingDisplayNameAttribute("Enable Custom Criteria")]
        public bool EnableCustomCriteria { get; set; }

        [SettingDisplayNameAttribute("Only Show Current System")]
        public bool OnlyShowCurrentSystem { get; set; }

        [SettingDisplayNameAttribute("Custom Criteria File")]
        [System.Text.Json.Serialization.JsonIgnore]
        public System.IO.FileInfo CustomCriteria {get => new System.IO.FileInfo(CustomCriteriaFile); set => CustomCriteriaFile = value.FullName;}

        [SettingIgnoreAttribute]
        public string CustomCriteriaFile { get; set; }
    }
}
