using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PipesSettingsManager : Singleton<PipesSettingsManager>
{

    Dictionary<PipeType, bool> pipeTypesSwitches;
    Dictionary<SubType, bool> subTypeSwitches;
    Dictionary<SubType, float[]> colors;
    Dictionary<SubType, Material> mats;

    public UnityEvent<SubType, bool> SubTypeChanged;
    public UnityEvent<PipeType, bool> PipeTypeChanged;
    public UnityEvent<PipeType> SubTypeOfThisTypeChanged;
    public UnityEvent<SubType, Color> ColorChangedSub;
    public UnityEvent<PipeType, Color> ColorChangedPipe;
    public UnityEvent ColorResetMade;
    public UnityEvent TypesResetMade;

    public enum state { all, some, none }
    public enum PipeType { Kanalizace = 1, Kolektor = 2, Plyn = 3, Voda = 4, Teplo = 5, SilnoProud = 6, SlaboProud = 7, Produkt = 8, Posta = 9 }
    
    //Takes the first, second and fifth number to make a unique combination (should be first to fifth based on the text, but for the dataset we recieved this was enough)
    public enum SubType
    {
        BezRozl = 103, Jednotna = 104, Odlehcovaci = 105, Destovat = 106, Splaskova = 107, Kalova = 109,
        Kolektor = 202, Reo = 288,
        BezRoz = 302, NTL = 303, STL = 304, VTL = 305, PKO = 306,
        BezRo = 405, VodovodPitna = 406, VodovodUzit = 407, VodovodLetni = 415,
        BezR = 501, Teplovod = 502, Horkovod = 503, Parovod = 504, Sekundarni = 505, Objekty = 525,
        Bez = 609, NN = 600, VN = 601, WN = 602, Zemni = 603, Verej = 610, Obj = 625,
        Be = 700, Antena = 703, Hodiny = 701, Dalkove =710, MistniTelefon = 720, TelefoniBudka = 725, Telefon = 730,
        Produkt = 800, ProduktObj = 825,
        Posta = 904
    }


    /// <summary>
    /// Read all the values form the player prefs
    /// </summary>
    private void Awake()
    {
        if (PlayerPrefs.HasKey("pipeTypesSwitches"))
        {
            string serialized = PlayerPrefs.GetString("pipeTypesSwitches");
            pipeTypesSwitches = JsonConvert.DeserializeObject< Dictionary<PipeType, bool>>(serialized);
        }
        else {
            pipeTypesSwitches = new Dictionary<PipeType, bool>();
            foreach (PipeType type in Enum.GetValues(typeof(PipeType))) {
                pipeTypesSwitches.Add(type, true);
            }
        }
        if (PlayerPrefs.HasKey("pipeSubTypesSwitches"))
        {
            string serialized = PlayerPrefs.GetString("pipeSubTypesSwitches");
            subTypeSwitches = JsonConvert.DeserializeObject<Dictionary<SubType, bool>>(serialized);
        }
        else
        {
            subTypeSwitches = new Dictionary<SubType, bool>();
            foreach (SubType type in Enum.GetValues(typeof(SubType)))
            {
                subTypeSwitches.Add(type, true);
            }
        }
        if (PlayerPrefs.HasKey("pipeColors"))
        {
            string serialized = PlayerPrefs.GetString("pipeColors");
            colors = JsonConvert.DeserializeObject<Dictionary<SubType, float[]>>(serialized);
        }
        else {
            colors = new Dictionary<SubType, float[]>();
            CreateColors();
        }
        mats = new Dictionary<SubType, Material>();
        CreateMaterials();
    }

    /// <summary>
    /// Create materials from the colors
    /// </summary>
    private void CreateMaterials() { 
        foreach(var sets in colors)
        {
            float[] colour = sets.Value;
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(colour[0], colour[1], colour[2]);
            mats[sets.Key] = mat;
        }
    }

    /// <summary>
    /// Changes the material based on a color
    /// </summary>
    private void ChangeMats()
    {
        foreach (var sets in colors)
        {
            float[] colour = sets.Value;
            mats[sets.Key].color = new Color(colour[0], colour[1], colour[2]);
        }
    }

    /// <summary>
    /// Saves the switches set by user
    /// </summary>
    public void SaveSwitches() {
        string serialized = JsonConvert.SerializeObject(pipeTypesSwitches);
        PlayerPrefs.SetString("pipeTypesSwitches", serialized);
        serialized = JsonConvert.SerializeObject(subTypeSwitches);
        PlayerPrefs.SetString("pipeSubTypesSwitches", serialized); 
    }


    /// <summary>
    /// Saves the colors set by the user
    /// </summary>
    private void SaveColors()
    {
        colors.Clear();


        foreach (var subMat in mats)
        {
            float[] colour = new float[3];
            colour[0] = subMat.Value.color.r;
            colour[1] = subMat.Value.color.g;
            colour[2] = subMat.Value.color.b;
            colors[subMat.Key] = colour;
        }
        string serialized = JsonConvert.SerializeObject(colors);
        PlayerPrefs.SetString("pipeColors", serialized);
    }


    /// <summary>
    /// Reset all the colours to their defaul values
    /// </summary>
    public void ResetColors() {
        colors.Clear();
        CreateColors();
        ChangeMats();
        ColorResetMade.Invoke();
        SaveColors();
    }
    /// <summary>
    /// Resets all the swithces to their default value (true)
    /// </summary>
    public void ResetSwitches() {
        pipeTypesSwitches.Clear();
        subTypeSwitches.Clear();
        foreach (PipeType type in Enum.GetValues(typeof(PipeType)))
        {
            pipeTypesSwitches.Add(type, true);
        }
        foreach (SubType type in Enum.GetValues(typeof(SubType)))
        {
            subTypeSwitches.Add(type, true);
        }
        TypesResetMade.Invoke();
        SaveSwitches();
    }


    /// <summary>
    /// Create the default colors for the pipes
    /// It is done it this way, because float[] can be serialized while Color can not
    /// </summary>
    private void CreateColors() {
       
        foreach (PipeType type in Enum.GetValues(typeof(PipeType))) {
            List<SubType> subtypes = GetSubTypes(type);
            switch (type) {
                case PipeType.Kanalizace:
                    foreach (SubType sub in subtypes) {
                        float[] colour = new float[3];
                        colour[0] = Color.green.r;
                        colour[1] = Color.green.g;
                        colour[2] = Color.green.b;
                        colors.Add(sub, colour); 
                    }
                    break;
                case PipeType.Kolektor:
                    foreach (SubType sub in subtypes)
                    {
                        float[] colour = new float[3];
                        colour[0] = Color.white.r;
                        colour[1] = Color.white.g;
                        colour[2] = Color.white.b;
                        colors.Add(sub, colour);
                    }
                    break;
                case PipeType.Plyn:
                    foreach (SubType sub in subtypes)
                    {
                        float[] colour = new float[3];
                        colour[0] = Color.yellow.r;
                        colour[1] = Color.yellow.g;
                        colour[2] = Color.yellow.b;
                        colors.Add(sub, colour);
                    }
                    break;
                case PipeType.Posta:
                    foreach (SubType sub in subtypes) {
                        float[] colour = new float[3];
                        colour[0] = Color.magenta.r;
                        colour[1] = Color.magenta.g;
                        colour[2] = Color.magenta.b;
                        colors.Add(sub, colour);
                    }
                    break;
                case PipeType.Produkt:
                    foreach (SubType sub in subtypes) {
                        float[] colour = new float[3];
                        colour[0] = Color.gray.r;
                        colour[1] = Color.gray.g;
                        colour[2] = Color.gray.b;
                        colors.Add(sub, colour);
                    }
                    break;
                case PipeType.SilnoProud:
                    foreach (SubType sub in subtypes)
                    {
                        float[] colour = new float[3];
                        colour[0] = Color.red.r;
                        colour[1] = Color.red.g;
                        colour[2] = Color.red.b;
                        colors.Add(sub, colour);
                    }
                    break;
                case PipeType.SlaboProud:
                    foreach (SubType sub in subtypes)
                    {
                        float[] colour = new float[3];
                        colour[0] = Color.black.r;
                        colour[1] = Color.black.g;
                        colour[2] = Color.black.b;
                        colors.Add(sub, colour);
                    }
                    break;
                case PipeType.Teplo:
                    foreach (SubType sub in subtypes)
                    {
                        float[] colour = new float[3];
                        colour[0] = Color.cyan.r;
                        colour[1] = Color.cyan.g;
                        colour[2] = Color.cyan.b;
                        colors.Add(sub, colour);
                    }
                    break;
                case PipeType.Voda:
                    foreach (SubType sub in subtypes)
                    {
                        float[] colour = new float[3];
                        colour[0] = Color.blue.r;
                        colour[1] = Color.blue.g;
                        colour[2] = Color.blue.b;
                        colors.Add(sub, colour);
                    }
                    break;


            }
        
        }
    
    
    
    }




    //-------------------Setters------------------------

    public void SetColor(Color color, SubType subtype) {
        ColorChangedSub.Invoke(subtype,color);
        mats[subtype].color = color;
        SaveColors();
    }

    public void SetColor(Color color, PipeType pipeType) {
        List<SubType> subtypes = GetSubTypes(pipeType);
        foreach(SubType sub in subtypes){
            mats[sub].color = color;
        }
        ColorChangedPipe.Invoke(pipeType, color);
        SaveColors();
    }


    public void SetSubTypeSwitch(SubType type, bool toSet)
    {
        subTypeSwitches[type] = toSet;
        SubTypeChanged.Invoke(type, toSet);
        SubTypeOfThisTypeChanged.Invoke(GetTypeFromSub(type));
    }



    public void SetPipeTypeSwitch(PipeType type, bool toSet)
    {
        pipeTypesSwitches[type] = toSet;
        PipeTypeChanged.Invoke(type, toSet);
    }




    // ------------------------ Getters------------------------
    public Color GetColor(PipeType pipeType) {
        List<SubType> subtypes = GetSubTypes(pipeType);
        return mats[subtypes[0]].color;
    }

    public Color GetColor(SubType subType) { 
        return mats[subType].color;
    }

    public static PipeType GetPipeType(string CTMTP)
    {
        int id = int.Parse(CTMTP[0].ToString());
        return (PipeType)id;
    }

    public static SubType GetPipeSubType(string CTMTP)
    {
        string part = string.Concat(CTMTP[0], CTMTP[1], CTMTP[4]);
        int id = int.Parse(part);
        return (SubType)id;
    }

    public List<SubType> GetSubTypes(PipeType type)
    {
        List<SubType> subtypes = new List<SubType>();

        foreach (SubType sub in Enum.GetValues(typeof(SubType)))
        {
            int value = (int)sub;
            int typeval = (int)type;
            if (value.ToString()[0] == typeval.ToString()[0])
            {
                subtypes.Add(sub);
            }
        }
        return subtypes;
    }

    public PipeType GetTypeFromSub(SubType subtype)
    {
        int value = (int)subtype;
        int type = int.Parse(value.ToString()[0].ToString());
        return (PipeType)type;
    }


    public bool GetSubtypeSwitch(SubType type)
    {
        return subTypeSwitches[type];
    }
    public bool GetTypeSwitch(PipeType type)
    {
        return pipeTypesSwitches[type];
    }
    public Material GetMaterial(SubType type)
    {
        return mats[type];
    }


}
