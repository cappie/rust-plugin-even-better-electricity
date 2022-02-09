using System.Collections.Generic;
using Newtonsoft.Json;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Even Better Electricity", "cappie", "1.0.0")]
    [Description("Allows even more control over electricity, including test generators!")]
	public class EvenBetterElectricity : RustPlugin
	{
		private const int LargeBatteryMaxDefault = 100;
		private const int MediumBatteryMaxDefault = 50;
		private const int SmallBatteryMaxDefault = 10;

		private static ElectricityConfiguration _configData;

		#region Configuration

		private class ElectricityConfiguration
		{
			[JsonProperty("chatIcon")]
			public ulong ChatIcon { get; set; }

			[JsonProperty("largeBatteryConfiguration")]
			public LargeBatteryConfiguration LargeBatteryConfiguration { get; set; }

			[JsonProperty("mediumBatteryConfiguration")]
			public MediumBatteryConfiguration MediumBatteryConfiguration { get; set; }

			[JsonProperty("smallBatteryConfiguration")]
			public SmallBatteryConfiguration SmallBatteryConfiguration { get; set; }

			[JsonProperty("windmillConfiguration")]
			public WindmillConfiguration WindmillConfiguration { get; set; }

			[JsonProperty("solarPanelConfiguration")]
			public SolarPanelConfiguration SolarPanelConfiguration { get; set; }

			[JsonProperty("smallGeneratorConfiguration")]
			public SmallGeneratorConfiguration SmallGeneratorConfiguration { get; set; }

			[JsonProperty("electricGeneratorConfiguration")]
			public ElectricGeneratorConfiguration ElectricGeneratorConfiguration { get; set; }

			public ElectricityConfiguration()
			{
				LargeBatteryConfiguration = new LargeBatteryConfiguration();
				MediumBatteryConfiguration = new MediumBatteryConfiguration();
				SmallBatteryConfiguration = new SmallBatteryConfiguration();

				WindmillConfiguration = new WindmillConfiguration();

				SolarPanelConfiguration = new SolarPanelConfiguration();

				SmallGeneratorConfiguration = new SmallGeneratorConfiguration();
				ElectricGeneratorConfiguration = new ElectricGeneratorConfiguration();
			}
		}

		private class BatteryConfiguration
		{
			[JsonProperty("maxOutput")]
			public int MaxOutput { get; set; }

			[JsonProperty("efficiency")]
			public float Efficiency { get; set; }

			[JsonProperty("maxCapacitySeconds")]
			public int MaxCapacitySeconds { get; set; }
		}

		private class LargeBatteryConfiguration : BatteryConfiguration
		{
			public LargeBatteryConfiguration()
			{
				MaxOutput = 100;
				Efficiency = 0.8f;
				MaxCapacitySeconds = 1440000;
			}
		}

		private class MediumBatteryConfiguration : BatteryConfiguration
		{
			public MediumBatteryConfiguration()
			{
				MaxOutput = 50;
				Efficiency = 0.8f;
				MaxCapacitySeconds = 540000;
			}
		}

		private class SmallBatteryConfiguration : BatteryConfiguration
		{
			public SmallBatteryConfiguration()
			{
				MaxOutput = 10;
				Efficiency = 0.8f;
				MaxCapacitySeconds = 9000;
			}
		}

		private class WindmillConfiguration
		{
			[JsonProperty("maxOutput")]
			public int MaxOutput { get; set; }

			public WindmillConfiguration()
			{
				MaxOutput = 150;
			}
		}

		private class SolarPanelConfiguration
		{
			[JsonProperty("maxOutput")]
			public int MaxOutput { get; set; }

			public SolarPanelConfiguration()
			{
				MaxOutput = 100;
			}
		}

		private class SmallGeneratorConfiguration
		{
			[JsonProperty("maxOutput")]
			public int MaxOutput { get; set; }

			public SmallGeneratorConfiguration()
			{
				MaxOutput = 75;
			}
		}

		private class ElectricGeneratorConfiguration
		{
			[JsonProperty("maxOutput")]
			public int MaxOutput { get; set; }

			public ElectricGeneratorConfiguration()
			{
				MaxOutput = 100;
			}
		}

		protected override void LoadConfig()
		{
			base.LoadConfig();

			try
			{
				_configData = Config.ReadObject<ElectricityConfiguration>();
			}
			catch
			{
				LoadDefaultConfig();
			}

			SaveConfig();
		}

		protected override void LoadDefaultConfig()
		{
			PrintWarning(GetLang("configurationCreate"));
			_configData = GetDefaultConfig();
		}

		protected override void SaveConfig()
		{
			Config.WriteObject(_configData);
		}

		private static ElectricityConfiguration GetDefaultConfig()
		{
			return new ElectricityConfiguration();
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Get the translated text.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="id"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		private string GetLang(string key, string id = null, params object[] args) =>
			string.Format(lang.GetMessage(key, this, id), args);

		#endregion

		#region Oxide Hooks

		private void OnServerInitialized()
		{
			ChangeSolarPanels();
			ChangeBatteries();
			ChangeWindmills();
			ChangeSmallGenerators();
			ChangeElectricGenerators();
		}

		private void Unload()
		{
			RevertSolarPanels();
			RevertBatteries();
			RevertWindmills();
			RevertSmallGenerators();
			RevertElectricGenerators();
		}

		private void OnEntitySpawned(BaseNetworkable networkObject)
		{
			var battery = networkObject.GetComponent<ElectricBattery>();
			if (battery != null)
			{
				AdjustBattery(battery);
			}

			var panel = networkObject.GetComponent<SolarPanel>();
			if (panel != null)
			{
				AdjustSolarPanel(panel);
			}

			var mill = networkObject.GetComponent<ElectricWindmill>();
			if (mill != null)
			{
				AdjustWindmill(mill);
			}

			var smallGenerator = networkObject.GetComponent<FuelGenerator>();
			if (smallGenerator != null)
			{
				AdjustSmallGenerator(smallGenerator);
			}

			var electricGenerator = networkObject.GetComponent<ElectricGenerator>();
			if (electricGenerator != null)
			{
				AdjustElectricGenerator(electricGenerator);
			}
		}

		#endregion

		#region Language

		protected override void LoadDefaultMessages()
		{
			lang.RegisterMessages(
				new Dictionary<string, string>
				{
					["chatPrefix"] = "<size=16><b>Better Electricity</b></size> <size=10>by p0mp.com</size>\n",
					["configurationCreate"] = "Configuration file is corrupt (or doesn't exists), creating new one!",
					["findBatteryAdjust"] = "Finding and adjusting all Batteries. (This may take some time)",
					["findBatteryRevert"] = "Finding and reverting all Batteries. (This may take some time)",
					["findElectricGeneratorAdjust"] = "Finding and adjusting all Electric Test Generators. (This may take some time)",
					["findElectricGeneratorRevert"] = "Finding and reverting all Electric Test Generators. (This may take some time)",
					["findWindmillAdjust"] = "Finding and adjusting all Mill Turbines. (This may take some time)",
					["findWindmillRevert"] = "Finding and reverting all Mill Turbines. (This may take some time)",
					["findSmallGeneratorAdjust"] = "Finding and adjusting all Small Generators. (This may take some time)",
					["findSmallGeneratorRevert"] = "Finding and reverting all Small Generators. (This may take some time)",
					["findSolarPanelsAdjust"] = "Finding and adjusting all Solar Panels. (This may take some time)",
					["findSolarPanelsRevert"] = "Finding and reverting all Solar Panels. (This may take some time)",
					["helpText"] = "We've increased the output for small/medium/large batteries, generators, solar panels and windmills.\n" +
						"  Batteries:\n" +
						"    Small batteries: {0}\n" +
						"    Medium batteries: {1}\n" +
						"    Large batteries: {2}\n" +
						"  Generators\n" +
						"    Electric generator: {3}\n" +
						"    Fuel generator: {4}\n" +
						"  Solar Panels: {5}\n" +
						"  Windmills: {6}",
				}, this
			);
		}

		#endregion

		#region Utilities

		private void Reload()
		{
			RevertBatteries();
			RevertElectricGenerators();
			RevertWindmills();
			RevertSmallGenerators();
			RevertSolarPanels();

			LoadConfig();

			ChangeBatteries();
			ChangeElectricGenerators();
			ChangeWindmills();
			ChangeSmallGenerators();
			ChangeSolarPanels();
		}

		#endregion

		#region Core

		private void ChangeSolarPanels()
		{
			// Heavy Initial Load
			Puts(GetLang("findSolarPanelsAdjust"));
			foreach (var panel in UnityEngine.Object.FindObjectsOfType<SolarPanel>())
			{
				AdjustSolarPanel(panel);
			}
		}

		private void ChangeBatteries()
		{
			// Heavy Initial Load
			Puts(GetLang("findBatteryAdjust"));
			foreach (var battery in UnityEngine.Object.FindObjectsOfType<ElectricBattery>())
			{
				AdjustBattery(battery);
			}
		}

		private void ChangeWindmills()
		{
			// Heavy Initial Load
			Puts(GetLang("findWindmillAdjust"));
			foreach (var mill in UnityEngine.Object.FindObjectsOfType<ElectricWindmill>())
			{
				AdjustWindmill(mill);
			}
		}

		private void ChangeSmallGenerators()
		{
			Puts(GetLang("findSmallGeneratorAdjust"));
			foreach (var generator in UnityEngine.Object.FindObjectsOfType<FuelGenerator>())
			{
				AdjustSmallGenerator(generator);
			}
		}

		private void ChangeElectricGenerators()
		{
			Puts(GetLang("findElectricGeneratorAdjust"));
			foreach (var generator in UnityEngine.Object.FindObjectsOfType<ElectricGenerator>())
			{
				AdjustElectricGenerator(generator);
			}
		}

		private void RevertBatteries()
		{
			Puts(GetLang("findBatteryRevert"));
			foreach (var battery in UnityEngine.Object.FindObjectsOfType<ElectricBattery>())
			{
				RevertBattery(battery);
			}
		}

		private void RevertSolarPanels()
		{
			Puts(GetLang("findSolarPanelsRevert"));
			foreach (var panel in UnityEngine.Object.FindObjectsOfType<SolarPanel>())
			{
				RevertSolarPanel(panel);
			}
		}

		private void RevertWindmills()
		{
			Puts(GetLang("findWindmillRevert"));
			foreach (var mill in UnityEngine.Object.FindObjectsOfType<ElectricWindmill>())
			{
				RevertWindmill(mill);
			}
		}

		private void RevertSmallGenerators()
		{
			Puts(GetLang("findSmallGeneratorRevert"));
			foreach (var generator in UnityEngine.Object.FindObjectsOfType<FuelElectricGenerator>())
			{
				RevertSmallGenerator(generator);
			}
		}

		private void RevertElectricGenerators()
		{
			Puts(GetLang("findElectricGeneratorRevert"));
			foreach (var generator in UnityEngine.Object.FindObjectsOfType<ElectricGenerator>())
			{
				RevertElectricGenerator(generator);
			}
		}

		private static void AdjustBattery(ElectricBattery battery)
		{
            // "maxCapactiySeconds" is not a typo.. It's something inside oxide's retarded shit

			if (battery.maxOutput == LargeBatteryMaxDefault)
			{
				// Large Battery
				battery.maxOutput = _configData.LargeBatteryConfiguration.MaxOutput;
				battery.maxCapactiySeconds = _configData.LargeBatteryConfiguration.MaxCapacitySeconds;
				battery.chargeRatio = _configData.LargeBatteryConfiguration.Efficiency;
				battery.maximumInboundEnergyRatio = _configData.LargeBatteryConfiguration.Efficiency * 10;
			}
			else if (battery.maxOutput == MediumBatteryMaxDefault)
			{
				battery.maxOutput = _configData.MediumBatteryConfiguration.MaxOutput;
				battery.maxCapactiySeconds = _configData.MediumBatteryConfiguration.MaxCapacitySeconds;
				battery.chargeRatio = _configData.MediumBatteryConfiguration.Efficiency;
				battery.maximumInboundEnergyRatio = _configData.MediumBatteryConfiguration.Efficiency * 10;
			}
			else if (battery.maxOutput == SmallBatteryMaxDefault)
			{
				// Small Battery.
				battery.maxOutput = _configData.SmallBatteryConfiguration.MaxOutput;
				battery.maxCapactiySeconds = _configData.SmallBatteryConfiguration.MaxCapacitySeconds;
				battery.chargeRatio = _configData.SmallBatteryConfiguration.Efficiency;
				battery.maximumInboundEnergyRatio = _configData.SmallBatteryConfiguration.Efficiency * 10;
			}

			battery.SendNetworkUpdate();
		}

		private static void AdjustSolarPanel(SolarPanel panel)
		{
			panel.maximalPowerOutput = _configData.SolarPanelConfiguration.MaxOutput;
		}

		private static void AdjustWindmill(ElectricWindmill mill)
		{
			mill.maxPowerGeneration = _configData.WindmillConfiguration.MaxOutput;
		}

		private static void AdjustSmallGenerator(FuelGenerator generator)
		{
			generator.outputEnergy = _configData.SmallGeneratorConfiguration.MaxOutput;
		}

		private static void AdjustElectricGenerator(ElectricGenerator generator)
		{
			generator.electricAmount = _configData.ElectricGeneratorConfiguration.MaxOutput;
		}

		private static void RevertBattery(ElectricBattery battery)
		{
			// Based on these values -> https://rust.facepunch.com/blog/november-update#batteryfixes
			if (battery.maxOutput == _configData.LargeBatteryConfiguration.MaxOutput)
			{
				battery.maxCapactiySeconds = 1440000;
				battery.chargeRatio = 0.8f;
				battery.maximumInboundEnergyRatio = 4;
				battery.maxOutput = LargeBatteryMaxDefault;
			}
			else if (battery.maxOutput == _configData.MediumBatteryConfiguration.MaxOutput)
			{
				battery.maxCapactiySeconds = 540000;
				battery.chargeRatio = 0.8f;
				battery.maximumInboundEnergyRatio = 4;
				battery.maxOutput = MediumBatteryMaxDefault;
			}
			else if (battery.maxOutput == _configData.SmallBatteryConfiguration.MaxOutput)
			{
				battery.maxCapactiySeconds = 9000;
				battery.chargeRatio = 0.8f;
				battery.maximumInboundEnergyRatio = 4;
				battery.maxOutput = SmallBatteryMaxDefault;
			}
			battery.SendNetworkUpdate(BasePlayer.NetworkQueue.Update);
		}

		private static void RevertSolarPanel(SolarPanel panel)
		{
			panel.maximalPowerOutput = 20;
		}

		private static void RevertWindmill(ElectricWindmill mill)
		{
			mill.maxPowerGeneration = 150;
		}

		private static void RevertSmallGenerator(FuelElectricGenerator generator)
		{
			generator.electricAmount = 40;
		}

		private static void RevertElectricGenerator(ElectricGenerator generator)
		{
			generator.electricAmount = 100;
		}

		#endregion

		#region Plugin hooks

		/// <summary>
		/// Implement the HelpText plugin hook.
		/// </summary>
		/// <param name="player"></param>
		[HookMethod("SendHelpText")]
		private void SendHelpText(BasePlayer player)
		{
			Player.Message(player, GetLang("chatPrefix") + GetLang("helpText", player.UserIDString, _configData.SmallBatteryConfiguration.MaxOutput, _configData.MediumBatteryConfiguration.MaxOutput, _configData.LargeBatteryConfiguration.MaxOutput, _configData.ElectricGeneratorConfiguration.MaxOutput, _configData.SmallGeneratorConfiguration.MaxOutput, _configData.SolarPanelConfiguration.MaxOutput, _configData.WindmillConfiguration.MaxOutput), null, _configData.ChatIcon);
		}

		#endregion
	}
}