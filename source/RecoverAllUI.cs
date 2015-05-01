using KerboKatz.Classes;
using KerboKatz.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace KerboKatz
{
  public partial class RecoverAll : KerboKatzBase
  {
    private bool initStyle;
    private GUIStyle areaStyle;
    private GUIStyle areaStyleHeader;
    private GUIStyle buttonStyle;
    private GUIStyle numberFieldStyle;
    private GUIStyle settingsWindowStyle;
    private GUIStyle textStyle;
    private GUIStyle textStyleShort;
    private GUIStyle textStyleShorter;
    private GUIStyle textStyleVesselHeader;
    private GUIStyle toggleStyle;
    private GUIStyle tooltipStyle;
    private GUIStyle verticalToolbar;
    private GUIStyle windowStyle;
    private int mainWindowID = 971300;
    private int settingsWindowID = 971301;
    private List<alignedTooltip> tooltipList = new List<alignedTooltip>();
    private Rectangle mainWindowRect = new Rectangle(Rectangle.updateType.Center);
    private Rectangle settingsWindowRect = new Rectangle(Rectangle.updateType.Cursor);
    private Vector2 mainWindowScroll = new Vector2();

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

      textStyleShort = new GUIStyle(textStyle);
      textStyleShort.fixedWidth = 73;
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

      initStyle = true;
    }

    public void OnGUI()
    {
      if (currentSettings.getBool("showWindow"))
      {
        if (!initStyle)
          InitStyle();
        Utilities.UI.createWindow(currentSettings.getBool("showWindow"), mainWindowID, ref mainWindowRect, mainWindow, "Recover All", windowStyle);
        Utilities.UI.createWindow(currentSettings.getBool("showSettings"), settingsWindowID, ref settingsWindowRect, settingsWindow, "Recover All Settings", settingsWindowStyle);
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
      var oldValue = currentSettings.getBool("includePrelaunch");
      var newValue = Utilities.UI.createToggle("Include prelaunch", oldValue, HighLogic.Skin.toggle, "If you enable this option vessels that arent launched, or are in a prelaunch state, will be ignored.");
      currentSettings.set("includePrelaunch", newValue);
      if (oldValue != newValue)
      {
        updateRecoverList();
      }
      GUILayout.EndVertical();
      Utilities.UI.updateTooltipAndDrag();
    }

    private void mainWindow(int windowID)
    {
      createVesselInfoHeader();
      mainWindowScroll = Utilities.UI.beginScrollView(mainWindowScroll, 590, 380, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, HighLogic.Skin.textArea);
      foreach (var currentVessel in vesselsToRecover)
      {
        createVesselInfoLayout(currentVessel, currentVessel.partTooltip, currentVessel.scienceTooltip, currentVessel.crewTooltip);
      }
      GUILayout.EndScrollView();
      GUILayout.BeginHorizontal();
      GUILayout.Space(buttonStyle.fixedWidth);
      GUILayout.FlexibleSpace();
      if (Utilities.UI.createButton("Recover all Vessels", buttonStyle))
      {
        recoverVessels();
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
      //part funds and tooltip
      Utilities.UI.createLabel("Vessel cost", textStyleShort, "Funding that you will get by recovering this vessel.");
      //science value and tooltip
      Utilities.UI.createLabel("Science", textStyleShort, "Science experiments that will be completed. This value is estimated. Real values may be lower/higher.");
      //crew members and tooltip
      Utilities.UI.createLabel("Crew", textStyleShorter, "Crew members that will be available again.");
      Utilities.UI.createLabel("Rate", textStyleShort, "Recovery rate depends on the distance to the KSC.");
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
      //part funds and tooltip
      Utilities.UI.createLabel((currentVessel.importantInfo.totalCost * currentVessel.importantInfo.distanceModifier).ToString("N2"), textStyleShort, partString);
      //science value and tooltip
      Utilities.UI.createLabel(currentVessel.importantInfo.totalScience.ToString("N2"), textStyleShort, scienceString);
      //crew members and tooltip
      Utilities.UI.createLabel(currentVessel.importantInfo.crewCount.ToString("N0"), textStyleShorter, crewString);
      //crew members and tooltip
      Utilities.UI.createLabel((currentVessel.importantInfo.distanceModifier * 100).ToString("N2") + "%", textStyleShort);
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