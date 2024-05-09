using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Xml.Serialization;
using System.ComponentModel;

public class TaleRequirement
{

}

public enum EResponseType
{
    Agree,
    Disagree,
    Silent
}

public class TaleResponse
{
    [XmlAttribute("type")] public EResponseType Type;
    [XmlAttribute("text")] public string Prompt;
    [XmlAttribute("isDefault")] public bool IsDefault = false;
    [XmlAttribute("waitTime")] public float WaitTime = 10f;

    [XmlText] public string Response;
}

public class TaleLine
{
    [XmlAttribute("promptDelay")] public float PromptDelay = 1f;
    [XmlAttribute("responseDelay")] public float ResponseDelay = 1.5f;
    [XmlAttribute("nextLineDelay")] public float NextLineDelay = 3f;

    public string Text;

    [XmlArray("Responses")]
    [XmlArrayItem("Response")]
    public TaleResponse[] Responses;
}

public enum ETaleType
{
    Template,
    Core
}

public class Tale
{
    [XmlAttribute("id")] public string UniqueID;
    [XmlAttribute("type")] public ETaleType Type;

    [XmlArray("Requirements")] [XmlArrayItem("Requirement")]
    public TaleRequirement[] Requirements;

    [XmlArray("Lines")] [XmlArrayItem("Line")]
    public TaleLine[] Lines;
}
