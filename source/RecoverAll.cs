using System;
using System.Collections.Generic;
using UnityEngine;

namespace KerboKatz
{
  [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
  public partial class RecoverAll : KerboKatzBase
  {
    public Dictionary<string, int> experimentCount               = new Dictionary<string, int>();
    public List<vesselInfo> vesselsToRecover                     = new List<vesselInfo>();
    public Dictionary<VesselType, vesselTypes> vesselTypesToShow = new Dictionary<VesselType, vesselTypes>();
    private double maxKSCDistance;
    public RecoverAll()
    {
      modName = "RecoverAll";
      requiresUtilities = new Version(1, 0, 4);
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
        string name = type.ToString();
        currentSettings.setDefault(name, "true");
        vesselTypesToShow.Add(type, new vesselTypes(type, name, currentSettings.getBool(name), this.updateRecoverList));
      }
    }

    public override void OnGuiAppLauncherReady()
    {
      base.OnGuiAppLauncherReady();
      maxKSCDistance = SpaceCenter.Instance.cb.Radius * Math.PI;
      button.Setup(toggleWindow, toggleWindow, Utilities.getTexture("icon", "RecoverAll/Textures"));
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
        updateRecoverList();
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

    private void updateRecoverList()
    {
      clearLists();
      var vessels = FlightGlobals.Vessels;
      foreach (var vessel in vessels)
      {
        if (!isRecoverable(vessel))
        {
          continue;
        }
        addVesselInfo(vessel);
      }
    }

    private void clearLists()
    {
      vesselsToRecover.Clear();
      experimentCount.Clear();
    }

    private bool isRecoverable(Vessel vessel)
    {
      if (vessel.mainBody != Planetarium.fetch.Sun.orbitingBodies[2] ||
        (!vessel.Landed &&
         !vessel.Splashed) ||
         (vessel.situation == Vessel.Situations.PRELAUNCH &&
         !currentSettings.getBool("includePrelaunch")) ||
         !vesselTypesToShow[vessel.vesselType].show)
      {
        return false;
      }
      return true;
    }

    private void addVesselInfo(Vessel vessel)
    {
      float totalCost = 0, totalScience = 0, partCost, science, crewCount = 0;
      float dryCost, fuelCost;
      var parts = new List<partInfo>();
      var scienceExperiments = new List<scienceInfo>();
      var crew = new List<ProtoCrewMember>();
      var distanceModifier = 1 - Utilities.toFloat(SpaceCenter.Instance.GreatCircleDistance(SpaceCenter.Instance.cb.GetRelSurfaceNVector(vessel.protoVessel.latitude, vessel.protoVessel.longitude)) / maxKSCDistance);
      foreach (var part in vessel.protoVessel.protoPartSnapshots)
      {
        totalCost += partCost = ShipConstruction.GetPartCosts(part, part.partInfo, out dryCost, out fuelCost);
        parts.Add(new partInfo(part.partInfo.title, partCost));

        foreach (ProtoPartModuleSnapshot partModule in part.modules)
        {
          if (partModule.moduleValues.HasNode("ScienceData"))
          {
            foreach (ConfigNode subjectNode in partModule.moduleValues.GetNodes("ScienceData"))
            {
              var scienceSubject = ResearchAndDevelopment.GetSubjectByID(subjectNode.GetValue("subjectID"));

              var experiment = ResearchAndDevelopment.GetExperiment(scienceSubject.id.Split('@')[0]);
              totalScience += science = Utilities.getScienceValue(experimentCount, experiment, scienceSubject);
              addToExperimentCount(experimentCount, scienceSubject);

              scienceExperiments.Add(new scienceInfo(scienceSubject, science, scienceSubject.title, Utilities.toFloat(subjectNode.GetValue("data"))));
            }
          }
        }
      }
      foreach (ProtoCrewMember protoCrew in vessel.protoVessel.GetVesselCrew())
      {
        crew.Add(protoCrew);
        crewCount++;
      }
      vesselsToRecover.Add(new vesselInfo(parts, scienceExperiments, crew, new importantInfo(vessel, vessel.vesselName, totalCost, totalScience, crewCount, distanceModifier, true)));
    }

    private void addToExperimentCount(Dictionary<string, int> experimentCount, ScienceSubject scienceSubject)
    {
      if (experimentCount.ContainsKey(scienceSubject.id))
      {
        experimentCount[scienceSubject.id] = experimentCount[scienceSubject.id] + 1;
      }
      else
      {
        experimentCount.Add(scienceSubject.id, 1);
      }
    }
    
    private void recoverVessels()
    {
      foreach (var currentVessel in vesselsToRecover)
      {
        if (!currentVessel.importantInfo.recover || currentVessel.importantInfo.vessel == null || !isRecoverable(currentVessel.importantInfo.vessel))
        {
          continue;
        }
        foreach (var currentScience in currentVessel.scienceInfo)
        {
          ResearchAndDevelopment.Instance.SubmitScienceData(currentScience.data, currentScience.subject);
        }
        foreach (var currentCrew in currentVessel.crewInfo)
        {
          currentCrew.rosterStatus = ProtoCrewMember.RosterStatus.Available;
        }

        addFunds(currentVessel.importantInfo.totalCost * currentVessel.importantInfo.distanceModifier, TransactionReasons.VesselRecovery);
        //set vessel type to debris so we dont get a reputation hit-- sadly doesnt work but do it anyways
        //currentVessel.importantInfo.vessel.vesselType = VesselType.Debris;

        HighLogic.CurrentGame.flightState.protoVessels.Remove(currentVessel.importantInfo.vessel.protoVessel);
        //currentVessel.importantInfo.vessel.Die();
        currentVessel.importantInfo.vessel.OnDestroy();
        Destroy(currentVessel.importantInfo.vessel);
        //workaround the reputation hit by adding it back still losing reputation but not as bad :)
        //Reputation.Instance.AddReputation(1, TransactionReasons.VesselRecovery);
      }
      updateRecoverList();
    }

    public void addFunds(float add, TransactionReasons reason)
    {
      if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER)
        return;
      Funding.Instance.AddFunds(add, reason);
    }
  }
}