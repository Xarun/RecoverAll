using KerboKatz.Classes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerboKatz
{
  [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
  public partial class DestroyAll : KerboKatzBase
  {
    public Dictionary<string, int> experimentCount               = new Dictionary<string, int>();
    public List<vesselInfo> vesselsToDestroy                     = new List<vesselInfo>();
    public Dictionary<VesselType, vesselTypes> vesselTypesToShow = new Dictionary<VesselType, vesselTypes>();
    public DestroyAll()
    {
      modName = "DestroyAll";
      requiresUtilities = new Version(1, 1, 0);
    }

    public override void Start()
    {
      base.Start();
      mainWindowRect.x     = currentSettings.getFloat("mainWindowRectX");
      mainWindowRect.y     = currentSettings.getFloat("mainWindowRectY");
      settingsWindowRect.x = currentSettings.getFloat("settingsWindowRectX");
      settingsWindowRect.y = currentSettings.getFloat("settingsWindowRectY");

      var vesselTypes = Utilities.GetValues<VesselType>();
      foreach (var type in vesselTypes)
      {
        /*if (type == VesselType.Unknown ||
            type == VesselType.SpaceObject)
          continue;*/
        string name = type.ToString();
        
        if (type == VesselType.Debris)
          currentSettings.setDefault(name, "true");
        else
          currentSettings.setDefault(name, "false");
        vesselTypesToShow.Add(type, new vesselTypes(type, name, currentSettings.getBool(name), this.updateDestroyList));
      }
    }

    public override void OnGuiAppLauncherReady()
    {
      base.OnGuiAppLauncherReady();
      button.Setup(toggleWindow, toggleWindow, Utilities.getTexture("icon", "DestroyAll/Textures"));
      button.VisibleInScenes = ApplicationLauncher.AppScenes.TRACKSTATION;
    }

    public void toggleWindow()
    {
      if (currentSettings.getBool("showWindow"))
      {
        clearLists();
        currentSettings.set("showWindow", false);
      }
      else
      {
        updateDestroyList();
        currentSettings.set("showWindow", true);
      }
    }

    public override void OnDestroy()
    {
      if (currentSettings != null)
      {
        currentSettings.set("showWindow", false);
        currentSettings.set("showSettings", false);
        currentSettings.set("mainWindowRectX", mainWindowRect.x);
        currentSettings.set("mainWindowRectY", mainWindowRect.y);
        currentSettings.set("settingsWindowRectX", settingsWindowRect.x);
        currentSettings.set("settingsWindowRectY", settingsWindowRect.y);

        foreach (var currentDic in vesselTypesToShow)
        {
          var current = currentDic.Value;
          currentSettings.set(current.name, current.show);
        }
        currentSettings.save();
      }
      GameEvents.onGUIApplicationLauncherReady.Remove(OnGuiAppLauncherReady);
      if (button != null)
      {
        ApplicationLauncher.Instance.RemoveModApplication(button);
      }
    }

    private void updateDestroyList()
    {
      clearLists();
      var vessels = FlightGlobals.Vessels;
      foreach (var vessel in vessels)
      {
        if (!isDestroyable(vessel))
        {
          continue;
        }
        Utilities.RecoverAll.addVesselInfo(vessel, ref experimentCount, ref vesselsToDestroy,true);
      }
    }

    private void clearLists()
    {
      vesselsToDestroy.Clear();
      experimentCount.Clear();
    }

    private bool isDestroyable(Vessel vessel)
    {
      if (!vesselTypesToShow[vessel.vesselType].show)
      {
        return false;
      }
      return true;
    } 
    private void destroyVessels()
    {
      foreach (var currentVessel in vesselsToDestroy)
      {
        if (!currentVessel.importantInfo.recover || currentVessel.importantInfo.vessel == null || !isDestroyable(currentVessel.importantInfo.vessel))
        {
          continue;
        }
        foreach (var currentCrew in currentVessel.crewInfo)
        {
          currentCrew.rosterStatus = ProtoCrewMember.RosterStatus.Dead;
        }
        HighLogic.CurrentGame.flightState.protoVessels.Remove(currentVessel.importantInfo.vessel.protoVessel);
        currentVessel.importantInfo.vessel.Die();
        Destroy(currentVessel.importantInfo.vessel);
      }
      updateDestroyList();
    }
  }
}