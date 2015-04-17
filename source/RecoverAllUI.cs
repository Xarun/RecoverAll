using System.Collections.Generic;
using UnityEngine;

namespace KerboKatz
{
  public partial class RecoverAll : KerboKatzBase
  {
    private bool initStyle;
    private float spaceSize;
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
    private int mainWindowID                                             = 971300;
    private int settingsWindowID                                         = 971301;
    private List<Tuple<string, float, string, float, float>> tooltipList = new List<Tuple<string, float, string, float, float>>();
    private Rect mainWindowRect                                          = new Rect();
    private Rect settingsWindowRect                                      = new Rect();
    private Vector2 mainWindowScroll                                     = new Vector2();

    private void InitStyle()
    {
      windowStyle = new GUIStyle(HighLogic.Skin.window);
      windowStyle.fixedWidth = 600;
      windowStyle.padding.left         = 0;

      settingsWindowStyle              = new GUIStyle(HighLogic.Skin.window);
      settingsWindowStyle.fixedWidth   = 200;

      textStyle                        = new GUIStyle(HighLogic.Skin.label);
      textStyle.fixedWidth             = 200;
      textStyle.margin.left            = 10;

      textStyleVesselHeader            = new GUIStyle(textStyle);
      textStyleVesselHeader.fixedWidth = 232;
      textStyleVesselHeader.alignment  = TextAnchor.MiddleCenter;

      textStyleShort                   = new GUIStyle(textStyle);
      textStyleShort.fixedWidth        = 73;
      textStyleShort.alignment         = TextAnchor.MiddleRight;

      textStyleShorter                 = new GUIStyle(textStyleShort);
      textStyleShorter.fixedWidth      = 50;

      numberFieldStyle                 = new GUIStyle(HighLogic.Skin.box);
      numberFieldStyle.fixedWidth      = 52;
      numberFieldStyle.fixedHeight     = 22;
      numberFieldStyle.alignment       = TextAnchor.MiddleCenter;
      numberFieldStyle.padding.right   = 7;
      numberFieldStyle.margin.top      = 5;

      buttonStyle                      = new GUIStyle(HighLogic.Skin.button);
      buttonStyle.fixedWidth           = 150;

      toggleStyle                      = new GUIStyle(HighLogic.Skin.toggle);
      toggleStyle.fixedWidth           = 20;
      toggleStyle.fixedHeight          = 20;

      areaStyle                        = new GUIStyle(HighLogic.Skin.button);
      areaStyle.fixedWidth             = 560;
      areaStyle.onHover                = areaStyle.normal;
      areaStyle.hover                  = areaStyle.normal;

      areaStyleHeader                  = new GUIStyle(areaStyle);
      areaStyleHeader.fixedWidth       = 590;

      verticalToolbar                  = new GUIStyle(GUI.skin.verticalScrollbar);
      verticalToolbar.fixedHeight      = 370;

      if (tooltipStyle == null)
      {
        tooltipStyle               = Utilities.getTooltipStyle();
        tooltipStyle.padding.left  = 0;
        tooltipStyle.padding.right = 0;
        spaceSize                  = tooltipStyle.CalcSize(new GUIContent("_ _")).x - (tooltipStyle.CalcSize(new GUIContent("_")).x * 2);
        if (float.IsInfinity(spaceSize) || spaceSize <= 0)
        {
          spaceSize = 1;
        }
        tooltipStyle.padding.left  = 5;
        tooltipStyle.padding.right = 5;
      }

      initStyle = true;
    }

    public void OnGUI()
    {
      if (currentSettings.getBool("showWindow"))
      {
        if (!initStyle)
          InitStyle();
        Utilities.createWindow(currentSettings.getBool("showWindow"), mainWindowID, ref mainWindowRect, mainWindow, "Recover All", windowStyle);
        Utilities.createWindow(currentSettings.getBool("showSettings"), settingsWindowID, ref settingsWindowRect, settingsWindow, "Recover All Settings", settingsWindowStyle);
        Utilities.showTooltip();
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
      var newValue = Utilities.createToggle("Include prelaunch", oldValue, HighLogic.Skin.toggle, "If you enable this option vessels that arent launched, or are in a prelaunch state, will be ignored.");
      currentSettings.set("includePrelaunch", newValue);
      if (oldValue != newValue)
      {
        updateRecoverList();
      }
      GUILayout.EndVertical();
      Utilities.updateTooltipAndDrag();
    }

    private void mainWindow(int windowID)
    {
      createVesselInfoHeader();
      mainWindowScroll = GUILayout.BeginScrollView(mainWindowScroll, false, true, GUIStyle.none, GUI.skin.verticalScrollbar, HighLogic.Skin.textArea, GUILayout.Width(590), GUILayout.Height(380));//420
      foreach (var currentVessel in vesselsToRecover)
      {
        createVesselInfo(currentVessel);
      }
      GUILayout.EndScrollView();
      GUILayout.BeginHorizontal();
      GUILayout.Space(buttonStyle.fixedWidth);
      GUILayout.FlexibleSpace();
      if (Utilities.createButton("Recover all Vessels", buttonStyle))
      {
        recoverVessels();
      }
      GUILayout.FlexibleSpace();
      if (Utilities.createButton("Settings", buttonStyle))
      {
        toggleSettings();
      }
      GUILayout.EndHorizontal();
      GUILayout.EndVertical();
      Utilities.updateTooltipAndDrag();
    }

    private void createVesselInfoHeader()
    {
      GUILayout.BeginVertical();
      GUILayout.BeginHorizontal(areaStyleHeader);
      Utilities.createLabel("Vessel name", textStyleVesselHeader);
      //part funds and tooltip
      Utilities.createLabel("Vessel cost", textStyleShort, "Funding that you will get by recovering this vessel");
      //science value and tooltip
      Utilities.createLabel("Science", textStyleShort, "Science experiment that will be completed. This value is estimated. Real values may be lower/higher");
      //crew members and tooltip
      Utilities.createLabel("Crew", textStyleShorter, "Crew members that will be available again");
      Utilities.createLabel("Rate", textStyleShort, "Recovery rate depends on the distance to the KSC");
      GUILayout.EndHorizontal();
    }

    private void createVesselInfo(vesselInfo currentVessel)
    {
      string partString = "";
      string scienceString = "";
      string crewString = "";
      float maxSize = 0;
      foreach (var currentPart in currentVessel.partInfo)
      {
        maxSize = addToTooltipList(maxSize, currentPart.name, currentPart.cost * currentVessel.importantInfo.distanceModifier);
      }
      partString = createOutString(ref maxSize);
      foreach (var currentScience in currentVessel.scienceInfo)
      {
        maxSize = addToTooltipList(maxSize, currentScience.title, currentScience.science);
      }
      scienceString = createOutString(ref maxSize);
      foreach (var currentCrew in currentVessel.crewInfo)
      {
        if (crewString != "")
          crewString += "\n";
        crewString += currentCrew.name;
      }
      createVesselInfoLayout(currentVessel, partString, scienceString, crewString);
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
      Utilities.createLabel(currentVessel.importantInfo.vesselName, textStyle);
      //part funds and tooltip
      Utilities.createLabel((currentVessel.importantInfo.totalCost * currentVessel.importantInfo.distanceModifier).ToString("N2"), textStyleShort, partString);
      //science value and tooltip
      Utilities.createLabel(currentVessel.importantInfo.totalScience.ToString("N2"), textStyleShort, scienceString);
      //crew members and tooltip
      Utilities.createLabel(currentVessel.importantInfo.crewCount.ToString("N0"), textStyleShorter, crewString);
      //crew members and tooltip
      Utilities.createLabel((currentVessel.importantInfo.distanceModifier * 100).ToString("N2") + "%", textStyleShort);
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

    private float addToTooltipList(float maxSize, string name, float value)
    {
      var costString = value.ToString("N2");
      var nameSize = tooltipStyle.CalcSize(new GUIContent(name));
      var costSize = tooltipStyle.CalcSize(new GUIContent(costString));
      var thisSize = nameSize.x + costSize.x;
      tooltipList.Add(new Tuple<string, float, string, float, float>(name, nameSize.x, costString, costSize.x, thisSize));
      if (thisSize > maxSize)
      {
        maxSize = thisSize;
      }
      return maxSize;
    }

    private string createOutString(ref float maxSize)
    {
      string outString = "";
      foreach (var current in tooltipList)
      {
        string spaces = "  ";
        if (current.Item5 < maxSize)
        {
          var toBeAdded = (maxSize - current.Item5) / spaceSize;
          for (int i = 0; i < toBeAdded; i++)
          {
            spaces += " ";
          }
        }
        if (outString != "")
          outString += "\n";
        outString += current.Item1 + ":" + spaces + current.Item3;
      }
      tooltipList.Clear();
      maxSize = 0;
      return outString;
    }
  }
}