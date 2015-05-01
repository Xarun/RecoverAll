using KerboKatz.Classes;
using KerboKatz.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace KerboKatz
{
  public partial class DestroyAll : KerboKatzBase
  {
    private bool initStyle;
    private GUIStyle areaStyle;
    private GUIStyle areaStyleHeader;
    private GUIStyle buttonStyle;
    private GUIStyle numberFieldStyle;
    private GUIStyle settingsWindowStyle;
    private GUIStyle textStyle;
    private GUIStyle textStyleShort;
    private GUIStyle textStyleVesselHeader;
    private GUIStyle toggleStyle;
    private GUIStyle tooltipStyle;
    private GUIStyle verticalToolbar;
    private GUIStyle windowStyle;
    private int mainWindowID = 971302;
    private int settingsWindowID = 971303;
    private List<alignedTooltip> tooltipList = new List<alignedTooltip>();
    private Rectangle mainWindowRect = new Rectangle(Rectangle.updateType.Center);
    private Rectangle settingsWindowRect = new Rectangle(Rectangle.updateType.Cursor);
    private Vector2 mainWindowScroll = new Vector2();
    private GUIStyle textStyleShorter;
    private Dictionary<Vessel.Situations, string> situations = new Dictionary<Vessel.Situations, string>();
    private void InitStyle()
    {
      windowStyle = new GUIStyle(HighLogic.Skin.window);
      windowStyle.fixedWidth = 600;
      windowStyle.padding.left = 0;

      settingsWindowStyle = new GUIStyle(HighLogic.Skin.window);
      settingsWindowStyle.fixedWidth = 200;

      textStyle = new GUIStyle(HighLogic.Skin.label);
      textStyle.fixedWidth = 200;
      textStyle.margin.left = 10;

      textStyleVesselHeader = new GUIStyle(textStyle);
      textStyleVesselHeader.fixedWidth = 232;
      textStyleVesselHeader.alignment = TextAnchor.MiddleCenter;
      textStyleVesselHeader.padding.right = 0;

      textStyleShort = new GUIStyle(textStyle);
      textStyleShort.fixedWidth = 90;
      textStyleShort.alignment = TextAnchor.MiddleRight;

      textStyleShorter = new GUIStyle(textStyleShort);
      textStyleShorter.fixedWidth = 50;

      numberFieldStyle = new GUIStyle(HighLogic.Skin.box);
      numberFieldStyle.fixedWidth = 52;
      numberFieldStyle.fixedHeight = 22;
      numberFieldStyle.alignment = TextAnchor.MiddleCenter;
      numberFieldStyle.padding.right = 7;
      numberFieldStyle.margin.top = 5;

      buttonStyle = new GUIStyle(HighLogic.Skin.button);
      buttonStyle.fixedWidth = 150;

      toggleStyle = new GUIStyle(HighLogic.Skin.toggle);
      toggleStyle.fixedWidth = 20;
      toggleStyle.fixedHeight = 20;

      areaStyle = new GUIStyle(HighLogic.Skin.button);
      areaStyle.fixedWidth = 560;
      areaStyle.onHover = areaStyle.normal;
      areaStyle.hover = areaStyle.normal;

      areaStyleHeader = new GUIStyle(areaStyle);
      areaStyleHeader.fixedWidth = 590;

      verticalToolbar = new GUIStyle(GUI.skin.verticalScrollbar);
      verticalToolbar.fixedHeight = 370;

      if (tooltipStyle == null)
      {
        tooltipStyle = new GUIStyle(Utilities.UI.getTooltipStyle());
        tooltipStyle.stretchWidth = true;
      }
      situations.Add(Vessel.Situations.DOCKED, "Docked.");
      situations.Add(Vessel.Situations.FLYING, "Flying.");
      situations.Add(Vessel.Situations.LANDED, "Landed.");
      situations.Add(Vessel.Situations.ORBITING, "Orbiting.");
      situations.Add(Vessel.Situations.PRELAUNCH, "Awaiting to launch.");
      situations.Add(Vessel.Situations.SPLASHED, "Splashed down.");
      situations.Add(Vessel.Situations.SUB_ORBITAL, "Sub orbital trajectory.");

      initStyle = true;
    }

    public void OnGUI()
    {
      if (currentSettings.getBool("showWindow"))
      {
        if (!initStyle)
          InitStyle();
        Utilities.UI.createWindow(currentSettings.getBool("showWindow"), mainWindowID, ref mainWindowRect, mainWindow, "Destroy All", windowStyle);
        Utilities.UI.createWindow(currentSettings.getBool("showSettings"), settingsWindowID, ref settingsWindowRect, settingsWindow, "Destroy All Settings", settingsWindowStyle);
        Utilities.UI.showTooltip(tooltipStyle);
      }
    }

    private void settingsWindow(int id)
    {
      GUILayout.BeginVertical();
      foreach (var currentDic in vesselTypesToShow)
      {
        var current = currentDic.Value;
        current.show = GUILayout.Toggle(current.show, current.name, HighLogic.Skin.toggle);
      }
      GUILayout.EndVertical();
      Utilities.UI.updateTooltipAndDrag();
    }

    private void mainWindow(int windowID)
    {
      createVesselInfoHeader();
      Utilities.UI.updateTooltipAndDrag(tooltipStyle, 200, false);
      mainWindowScroll = Utilities.UI.beginScrollView(mainWindowScroll, 590, 380, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, HighLogic.Skin.textArea);
      foreach (var currentVessel in vesselsToDestroy)
      {
        createVesselInfoLayout(currentVessel, currentVessel.partTooltip, currentVessel.scienceTooltip, currentVessel.crewTooltip);
      }
      GUILayout.EndScrollView();
      GUILayout.BeginHorizontal();
      GUILayout.Space(buttonStyle.fixedWidth);
      GUILayout.FlexibleSpace();
      if (Utilities.UI.createButton("Destroy all Vessels", buttonStyle))
      {
        destroyVessels();
      }
      GUILayout.FlexibleSpace();
      if (Utilities.UI.createButton("Settings", buttonStyle))
      {
        toggleSettings();
      }
      GUILayout.EndHorizontal();
      GUILayout.EndVertical();
      Utilities.UI.updateTooltipAndDrag(tooltipStyle, 500);
    }

    private void createVesselInfoHeader()
    {
      GUILayout.BeginVertical();
      GUILayout.BeginHorizontal(areaStyleHeader);
      Utilities.UI.createLabel("Vessel name", textStyleVesselHeader);
      Utilities.UI.createLabel("Main body", textStyleShort, "Main body and state of this vessel.");
      //part funds and tooltip
      Utilities.UI.createLabel("Vessel cost", textStyleShort, "Funding that you will lose by destroying this vessel.");
      //science value and tooltip
      Utilities.UI.createLabel("Science", textStyleShorter, "Science experiments that will be lost. This value is estimated. Real values may be lower/higher.");
      //crew members and tooltip
      Utilities.UI.createLabel("Crew", textStyleShorter, "Crew members that will die when you destroy this vessel.");
      GUILayout.EndHorizontal();
    }

    private void createVesselInfoLayout(vesselInfo currentVessel, string partString, string scienceString, string crewString)
    {
      GUILayout.BeginHorizontal(areaStyle);
      if (GUILayout.Toggle(currentVessel.importantInfo.recover, "", toggleStyle))
      {
        currentVessel.importantInfo.recover = true;
      }
      else
      {
        currentVessel.importantInfo.recover = false;
      }
      Utilities.UI.createLabel(currentVessel.importantInfo.vesselName, textStyle);
      Utilities.UI.createLabel(currentVessel.importantInfo.vessel.mainBody.name, textStyleShort, situations[currentVessel.importantInfo.vessel.situation]);
      //part funds and tooltip
      Utilities.UI.createLabel((currentVessel.importantInfo.totalCost).ToString("N2"), textStyleShort, partString);
      //science value and tooltip
      Utilities.UI.createLabel(currentVessel.importantInfo.totalScience.ToString("N2"), textStyleShorter, scienceString);
      //crew members and tooltip
      Utilities.UI.createLabel(currentVessel.importantInfo.crewCount.ToString("N0"), textStyleShorter, crewString);
      GUILayout.EndHorizontal();
    }

    private void toggleSettings()
    {
      if (currentSettings.getBool("showSettings"))
      {
        currentSettings.set("showSettings", false);
      }
      else
      {
        currentSettings.set("showSettings", true);
      }
    }
  }
}