using System;
using System.Collections.Generic;

namespace KerboKatz
{
  public class vesselInfo
  {
    public List<partInfo> partInfo;
    public List<scienceInfo> scienceInfo;
    public List<ProtoCrewMember> crewInfo;
    public importantInfo importantInfo;
    public vesselInfo(List<partInfo> partInfo, List<scienceInfo> scienceInfo, List<ProtoCrewMember> crewInfo, importantInfo importantInfo)
    {
      this.partInfo = partInfo;
      this.scienceInfo = scienceInfo;
      this.crewInfo = crewInfo;
      this.importantInfo = importantInfo;
    }
  }

  public class partInfo
  {
    public string name;
    public float cost;
    public partInfo(string name, float cost)
    {
      this.name = name;
      this.cost = cost;
    }
  }

  public class scienceInfo
  {
    public ScienceSubject subject;
    public float science;
    public string title;
    public float data;
    public scienceInfo(ScienceSubject subject, float science, string title, float data)
    {
      this.subject = subject;
      this.science = science;
      this.title   = title;
      this.data    = data;
    }
  }

  public class importantInfo
  {
    public Vessel vessel;
    public string vesselName;
    public float totalCost;
    public float totalScience;
    public float crewCount;
    public bool recover;
    public float distanceModifier;
    public importantInfo(Vessel vessel, string vesselName, float totalCost, float totalScience, float crewCount, float distanceModifier, bool recover)
    {
      this.vessel           = vessel;
      this.vesselName       = vesselName;
      this.totalCost        = totalCost;
      this.totalScience     = totalScience;
      this.crewCount        = crewCount;
      this.recover          = recover;
      this.distanceModifier = distanceModifier;
    }
  }

  public class vesselTypes
  {
    public VesselType vesselType;
    public string name;
    //public bool show;
    public Action callback;
    public bool init;
    public vesselTypes(VesselType vesselType, string name, bool show, Action callback)
    {
      this.vesselType = vesselType;
      this.name       = name;
      this.show       = show;
      this.callback   = callback;
      init            = true;
    }

    //public bool show { get; set; }
    private bool _show;

    public bool show
    {
      get { return _show; }
      set
      {
        var old = _show;
        _show = value;
        if (init && old != _show)
        {
          callback();
        }
      }
    }
  }
}