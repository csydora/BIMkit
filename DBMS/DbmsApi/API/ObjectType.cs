using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DbmsApi.Mongo;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DbmsApi.API
{
    public class ObjectType
    {
        [BsonId]
        [BsonElement("Name")]
        public string Name;
        public string ParentName;
        public ObjectType(string name, string parentID)
        {
            this.Name = name;
            this.ParentName = parentID;
        }

        public bool Equals(ObjectType obj)
        {
            return this.Name == obj.Name;
        }

        public override string ToString()
        {
            return Name;
        }

        public static bool RecusiveTypeCheck(ObjectType checkType, ObjectType objType)
        {
            // Checks if the objectType or any of its parents are equal to the checkType
            if (objType == null)
            {
                return false;
            }
            if (objType.Name == checkType.Name)
            {
                return true;
            }
            return RecusiveTypeCheck(checkType, ObjectTypeTree.GetType(objType.ParentName));
        }
    }

    public static class ObjectTypeTree
    {
        private static Dictionary<string, ObjectType> ObjectDict;
        private static Dictionary<string, ObjectType> ObjectParentDict;
        private static Dictionary<string, List<ObjectType>> ObjectChildDict;

        public static ObjectType GetType(string id)
        {
            if (id == null)
            {
                return null;
            }
            if (ObjectDict.TryGetValue(id, out ObjectType type))
            {
                return type;
            }
            return null;
        }
        public static List<ObjectType> GetTypeChildren(string id)
        {
            if (ObjectChildDict.TryGetValue(id, out List<ObjectType> types))
            {
                return types;
            }
            return null;
        }
        public static List<ObjectType> GetAllTypes()
        {
            return ObjectDict.Values.ToList();
        }
        public static List<ObjectType> GetAllLeafTypes()
        {
            return ObjectDict.Values.Where(t=>ObjectChildDict[t.Name].Count == 0).ToList();
        }

        public static void BuildTypeTree(List<ObjectType> types)
        {
            ObjectDict = new Dictionary<string, ObjectType>();
            ObjectParentDict = new Dictionary<string, ObjectType>();
            ObjectChildDict = new Dictionary<string, List<ObjectType>>();
            foreach (ObjectType type in types)
            {
                ObjectDict.Add(type.Name, type);
                ObjectChildDict[type.Name] = new List<ObjectType>();
            }
            foreach (ObjectType type in types)
            {
                if (type.ParentName != null)
                {
                    ObjectParentDict.Add(type.Name, ObjectDict[type.ParentName]);
                    ObjectChildDict[type.ParentName].Add(type);
                }
            }
        }

        private static List<ObjectType> tempList;
        private static ObjectType CreateNew(string name, string parentName)
        {
            ObjectType temp = new ObjectType(name, parentName);
            tempList.Add(temp);
            return temp;
        }
        public static List<ObjectType> DefaultTypesList()
        {
            tempList = new List<ObjectType>();

            ObjectType Root = CreateNew("Root", null);
            ObjectType Virtual = CreateNew("Virtual", Root.Name);
            ObjectType Building = CreateNew("Building", Virtual.Name);
            ObjectType BuildingStorey = CreateNew("BuildingStorey", Virtual.Name);
            ObjectType Corner = CreateNew("Corner", Virtual.Name);
            ObjectType FurnishingCoM = CreateNew("FurnishingCoM", Virtual.Name);
            ObjectType KitchenIsland = CreateNew("KitchenIsland", Virtual.Name);
            ObjectType KitchenPeninsula = CreateNew("KitchenPeninsula", Virtual.Name);
            ObjectType KitchenLandingArea = CreateNew("KitchenLandingArea", Virtual.Name);
            ObjectType KitchenWorkingTriangle = CreateNew("KitchenWorkingTriangle", Virtual.Name);
            ObjectType OpenSpace = CreateNew("OpenSpace", Virtual.Name);
            ObjectType Room = CreateNew("Room", Virtual.Name);
            ObjectType RoomCentroid = CreateNew("RoomCentroid", Virtual.Name);
            ObjectType Site = CreateNew("Site", Virtual.Name);
            ObjectType WalkPath = CreateNew("WalkPath", Virtual.Name);

            ObjectType Real = CreateNew("Real", Root.Name);
            ObjectType BuildingElement = CreateNew("BuildingElement", Real.Name);
            ObjectType Opening = CreateNew("Opening", BuildingElement.Name);
            ObjectType Beam = CreateNew("Beam", BuildingElement.Name);
            //      IfcBeamStandardCase
            // IfcBuildingElementProxy
            ObjectType Joist = CreateNew("Joist", Beam.Name);
            ObjectType Chimney = CreateNew("Chimney", BuildingElement.Name);
            ObjectType Column = CreateNew("Column", BuildingElement.Name);
            //      IfcColumnStandardCase
            ObjectType Stud = CreateNew("Stud", Column.Name);
            ObjectType Covering = CreateNew("Covering", BuildingElement.Name);
            ObjectType CurtainWall = CreateNew("CurtainWall", BuildingElement.Name);
            ObjectType Door = CreateNew("Door", Opening.Name);
            //      IfcDoorStandardCase
            ObjectType Footing = CreateNew("Footing", BuildingElement.Name);
            ObjectType Member = CreateNew("Member", BuildingElement.Name);
            //      IfcMemberStandardCase
            ObjectType Pile = CreateNew("Pile", BuildingElement.Name);
            ObjectType Plate = CreateNew("Plate", BuildingElement.Name);
            //      IfcPlateStandardCase
            ObjectType Railing = CreateNew("Railing", BuildingElement.Name);
            ObjectType Ramp = CreateNew("Ramp", BuildingElement.Name);
            ObjectType RampFlight = CreateNew("RampFlight", BuildingElement.Name);
            ObjectType Roof = CreateNew("Roof", BuildingElement.Name);
            ObjectType ShadingDevice = CreateNew("ShadingDevice", BuildingElement.Name);
            ObjectType Slab = CreateNew("Slab", BuildingElement.Name);
            //      IfcSlabElementedCase
            //      IfcSlabStandardCase
            ObjectType CounterTop = CreateNew("CounterTop", Slab.Name);
            ObjectType Floor = CreateNew("Floor", Slab.Name);
            ObjectType Stair = CreateNew("Stair", BuildingElement.Name);
            ObjectType StairFlight = CreateNew("StairFlight", BuildingElement.Name);
            ObjectType Wall = CreateNew("Wall", Slab.Name);
            //      IfcWallElementedCase
            //      IfcWallStandardCase
            ObjectType Window = CreateNew("Window", Opening.Name);
            //      IfcWindowStandardCase
            ObjectType IfcCivilElement = CreateNew("IfcCivilElement", Real.Name);
            ObjectType DistributionElement = CreateNew("DistributionElement", Real.Name);
            ObjectType DistributionControlElement = CreateNew("DistributionControlElement", DistributionElement.Name);
            ObjectType Actuator = CreateNew("Actuator", DistributionControlElement.Name);
            ObjectType Alarm = CreateNew("Alarm", DistributionControlElement.Name);
            ObjectType Controller = CreateNew("Controller", DistributionControlElement.Name);
            ObjectType Sensor = CreateNew("Sensor", DistributionControlElement.Name);
            ObjectType FlowInstrument = CreateNew("FlowInstrument", DistributionControlElement.Name);
            ObjectType ProtectiveDeviceTrippingUnit = CreateNew("ProtectiveDeviceTrippingUnit", DistributionControlElement.Name);
            ObjectType UnitaryControlElement = CreateNew("UnitaryControlElement", DistributionControlElement.Name);
            ObjectType DistributionFlowElement = CreateNew("DistributionFlowElement", DistributionElement.Name);
            ObjectType DistributionChamberElement = CreateNew("DistributionChamberElement", DistributionFlowElement.Name);
            ObjectType EnergyConversionDevice = CreateNew("EnergyConversionDevice", DistributionFlowElement.Name);
            ObjectType AirToAirHeatRecovery = CreateNew("AirToAirHeatRecovery", EnergyConversionDevice.Name);
            ObjectType Boiler = CreateNew("Boiler", EnergyConversionDevice.Name);
            ObjectType Burner = CreateNew("Burner", EnergyConversionDevice.Name);
            ObjectType Chiller = CreateNew("Chiller", EnergyConversionDevice.Name);
            ObjectType Coil = CreateNew("Coil", EnergyConversionDevice.Name);
            ObjectType Condenser = CreateNew("Condenser", EnergyConversionDevice.Name);
            ObjectType CooledBeam = CreateNew("CooledBeam", EnergyConversionDevice.Name);
            ObjectType CoolingTower = CreateNew("CoolingTower", EnergyConversionDevice.Name);
            ObjectType ElectricGenerator = CreateNew("ElectricGenerator", EnergyConversionDevice.Name);
            ObjectType ElectricMotor = CreateNew("ElectricMotor", EnergyConversionDevice.Name);
            ObjectType Engine = CreateNew("Engine", EnergyConversionDevice.Name);
            ObjectType EvaporativeCooler = CreateNew("EvaporativeCooler", EnergyConversionDevice.Name);
            ObjectType Evaporator = CreateNew("Evaporator", EnergyConversionDevice.Name);
            ObjectType HeatExchanger = CreateNew("HeatExchanger", EnergyConversionDevice.Name);
            ObjectType Humidifier = CreateNew("Humidifier", EnergyConversionDevice.Name);
            ObjectType MotorConnection = CreateNew("MotorConnection", EnergyConversionDevice.Name);
            ObjectType SolarDevice = CreateNew("SolarDevice", EnergyConversionDevice.Name);
            ObjectType Transformer = CreateNew("Transformer", EnergyConversionDevice.Name);
            ObjectType TubeBundle = CreateNew("TubeBundle", EnergyConversionDevice.Name);
            ObjectType UnitaryEquipment = CreateNew("UnitaryEquipment", EnergyConversionDevice.Name);
            ObjectType FlowController = CreateNew("FlowController", DistributionFlowElement.Name);
            ObjectType AirTerminalBox = CreateNew("AirTerminalBox", FlowController.Name);
            ObjectType Damper = CreateNew("Damper", FlowController.Name);
            ObjectType ElectricDistributionBoard = CreateNew("ElectricDistributionBoard", FlowController.Name);
            ObjectType ElectricTimeControl = CreateNew("ElectricTimeControl", FlowController.Name);
            ObjectType FlowMeter = CreateNew("FlowMeter", FlowController.Name);
            ObjectType ProtectiveDevice = CreateNew("ProtectiveDevice", FlowController.Name);
            ObjectType SwitchingDevice = CreateNew("SwitchingDevice", FlowController.Name);
            ObjectType Valve = CreateNew("Valve", FlowController.Name);
            ObjectType FlowFitting = CreateNew("FlowFitting", DistributionFlowElement.Name);
            ObjectType CableCarrierFitting = CreateNew("CableCarrierFitting", FlowFitting.Name);
            ObjectType CableFitting = CreateNew("CableFitting", FlowFitting.Name);
            ObjectType DuctFitting = CreateNew("DuctFitting", FlowFitting.Name);
            ObjectType JunctionBox = CreateNew("JunctionBox", FlowFitting.Name);
            ObjectType PipeFitting = CreateNew("PipeFitting", FlowFitting.Name);
            ObjectType FlowMovingDevice = CreateNew("FlowMovingDevice", DistributionFlowElement.Name);
            ObjectType Compressor = CreateNew("Compressor", FlowMovingDevice.Name);
            ObjectType Fan = CreateNew("Fan", FlowMovingDevice.Name);
            ObjectType Pump = CreateNew("Pump", FlowMovingDevice.Name);
            ObjectType FlowSegment = CreateNew("FlowSegment", DistributionFlowElement.Name);
            ObjectType CableCarrierSegment = CreateNew("CableCarrierSegment", FlowSegment.Name);
            ObjectType CableSegment = CreateNew("CableSegment", FlowSegment.Name);
            ObjectType DuctSegment = CreateNew("DuctSegment", FlowSegment.Name);
            ObjectType PipeSegment = CreateNew("PipeSegment", FlowSegment.Name);
            ObjectType FlowStorageDevice = CreateNew("FlowStorageDevice", DistributionChamberElement.Name);
            ObjectType ElectricFlowStorageDevice = CreateNew("ElectricFlowStorageDevice", FlowStorageDevice.Name);
            ObjectType Battery = CreateNew("Battery", ElectricFlowStorageDevice.Name);
            ObjectType Tank = CreateNew("Tank", FlowStorageDevice.Name);
            ObjectType FlowTerminal = CreateNew("FlowTerminal", DistributionChamberElement.Name);
            ObjectType AirTerminal = CreateNew("AirTerminal", FlowTerminal.Name);
            ObjectType AudioVisualAppliance = CreateNew("AudioVisualAppliance", FlowTerminal.Name);
            ObjectType Radio = CreateNew("Radio", AudioVisualAppliance.Name);
            ObjectType Television = CreateNew("Television", AudioVisualAppliance.Name);
            ObjectType Speakers = CreateNew("Speakers", AudioVisualAppliance.Name);
            ObjectType CommunicationsAppliance = CreateNew("CommunicationsAppliance", FlowTerminal.Name);
            ObjectType Telephone = CreateNew("Telephone", CommunicationsAppliance.Name);
            ObjectType ElectricAppliance = CreateNew("ElectricAppliance", FlowTerminal.Name);
            ObjectType Range = CreateNew("Range", ElectricAppliance.Name);
            ObjectType Stove = CreateNew("Stove", ElectricAppliance.Name);
            ObjectType Oven = CreateNew("Oven", ElectricAppliance.Name);
            ObjectType CookTop = CreateNew("CookTop", ElectricAppliance.Name);
            ObjectType Refrigerator = CreateNew("Refrigerator", ElectricAppliance.Name);
            ObjectType Microwave = CreateNew("Microwave", ElectricAppliance.Name);
            ObjectType Dyer = CreateNew("Dyer", ElectricAppliance.Name);
            ObjectType FireSuppressionTerminal = CreateNew("FireSuppressionTerminal", FlowTerminal.Name);
            ObjectType LightFixture = CreateNew("LightFixture", FlowTerminal.Name);
            ObjectType Lamp = CreateNew("Lamp", LightFixture.Name);
            ObjectType MedicalDevice = CreateNew("MedicalDevice", FlowTerminal.Name);
            ObjectType Outlet = CreateNew("Outlet", FlowTerminal.Name);
            ObjectType SanitaryTerminal = CreateNew("SanitaryTerminal", FlowTerminal.Name);
            ObjectType Sink = CreateNew("Sink", SanitaryTerminal.Name);
            ObjectType Shower = CreateNew("Shower", SanitaryTerminal.Name);
            ObjectType Toilet = CreateNew("Toilet", SanitaryTerminal.Name);
            ObjectType Bath = CreateNew("Bath", SanitaryTerminal.Name);
            ObjectType WashingMashine = CreateNew("WashingMashine", SanitaryTerminal.Name);
            ObjectType Dishwasher = CreateNew("Dishwasher", SanitaryTerminal.Name);
            ObjectType SpaceHeater = CreateNew("SpaceHeater", FlowTerminal.Name);
            ObjectType StackTerminal = CreateNew("StackTerminal", FlowTerminal.Name);
            ObjectType WasteTerminal = CreateNew("WasteTerminal", FlowTerminal.Name);
            ObjectType FlowTreatmentDevice = CreateNew("FlowTreatmentDevice", DistributionFlowElement.Name);
            ObjectType DuctSilencer = CreateNew("DuctSilencer", DistributionFlowElement.Name);
            ObjectType Filter = CreateNew("Filter", DistributionFlowElement.Name);
            ObjectType Interceptor = CreateNew("Interceptor", DistributionFlowElement.Name);

            ObjectType FurnishingElement = CreateNew("FurnishingElement", Real.Name);
            ObjectType Couch = CreateNew("Couch", FurnishingElement.Name);
            ObjectType Chair = CreateNew("Chair", FurnishingElement.Name);
            ObjectType Table = CreateNew("Table", FurnishingElement.Name);
            ObjectType CoffeeTable = CreateNew("CoffeeTable", Table.Name);
            ObjectType KitchenTable = CreateNew("KitchenTable", Table.Name);
            ObjectType Bed = CreateNew("Bed", FurnishingElement.Name);
            ObjectType Plant = CreateNew("Plant", FurnishingElement.Name);
            ObjectType Cabinet = CreateNew("Cabinet", FurnishingElement.Name);
            ObjectType CornerCabinet = CreateNew("CornerCabinet", Cabinet.Name);
            ObjectType BaseCornerCabinet = CreateNew("BaseCornerCabinet", CornerCabinet.Name);
            ObjectType WallCornerCabinet = CreateNew("WallCornerCabinet", CornerCabinet.Name);
            ObjectType WallCabinet = CreateNew("WallCabinet", Cabinet.Name);
            ObjectType BaseCabinet = CreateNew("BaseCabinet", Cabinet.Name);
            ObjectType Shelf = CreateNew("Shelf", FurnishingElement.Name);
            ObjectType TVStand = CreateNew("TVStand", FurnishingElement.Name);
            ObjectType Container = CreateNew("Container", FurnishingElement.Name);

            return tempList;
        }

        public static bool CreatesLoop(List<ObjectType> types, ObjectType current)
        {
            if (ObjectDict.Count == 0)
            {
                BuildTypeTree(types);
            }
            string startName = current.Name;
            current = GetType(current.ParentName);
            while (current.Name != startName && current.Name != "Root")
            {
                current = GetType(current.ParentName);
            }

            return current.Name != "Root";
        }
    }
}

public enum ObjectTypes
{
    Root = 1,

    Virtual = 2,
    Building = 3,
    BuildingStorey = 4,
    Corner = 5,
    FurnishingCoM = 6,
    KitchenIsland = 7,
    KitchenPeninsula = 8,
    KitchenLandingArea = 9,
    KitchenWorkingTriangle = 10,
    OpenSpace = 11,
    Room = 12,
    RoomCentroid = 13,
    Site = 14,
    WalkPath = 15,
    Real = 100,
    BuildingElement = 101,
    Beam = 102,
    Joist = 103,
    Chimney = 107,
    Column = 108,
    Stud = 109,
    Covering = 110,
    Footing = 111,
    Member = 200,
    Opening = 201,
    Door = 202,
    Window = 210,
    Pile = 230,
    Plate = 231,
    Railing = 232,
    Ramp = 233,
    RampFlight = 234,
    Roof = 235,
    ShadingDevice = 245,
    Slab = 250,
    CounterTop = 251,
    Floor = 252,
    Wall = 260,
    CurtainWall = 261,
    Stair = 280,
    StairFlight = 281,
    FurnishingElement = 500,
    Couch = 501,
    Chair = 510,
    Table = 520,
    CoffeeTable = 521,
    KitchenTable = 522,
    Bed = 530,
    Plant = 540,
    Cabinet = 550,
    WallCabinet = 551,
    CornerCabinet = 552,
    BaseCornerCabinet = 553,
    WallCornerCabinet = 554,
    BaseCabinet = 555,
    Shelf = 570,
    TVStand = 575,
    Container = 580,
    DistributionElement = 1000,
    DistributionControlElement = 1001,
    Actuator,
    Alarm = 1010,
    Controller,
          FlowInstrument,
          ProtectiveDeviceTrippingUnit,
    Sensor = 1050,
    UnitaryControlElement,
    DistributionFlowElement = 1200,
    DistributionChamberElement = 1201,
    EnergyConversionDevice,
                  AirToAirHeatRecovery,
                  Boiler,
                  Burner,
                  Chiller,
                  Coil,
                  Condenser,
                  CooledBeam,
                  CoolingTower,
                  ElectricGenerator,
                  ElectricMotor,
                  Engine,
                  EvaporativeCooler,
                  Evaporator,
                  HeatExchanger,
                  Humidifier,
                  MotorConnection,
                  SolarDevice,
                  Transformer,
                  TubeBundle,
                  UnitaryEquipment,
              FlowController,
                  AirTerminalBox,
                  Damper,
                  ElectricDistributionBoard,
                  ElectricTimeControl,
                  FlowMeter,
                  ProtectiveDevice,
                  SwitchingDevice,
                  Valve,
              FlowFitting,
                  CableCarrierFitting,
                  CableFitting,
                  DuctFitting,
                  JunctionBox,
                  PipeFitting,
              FlowMovingDevice,
                  Compressor,
                  Fan,
                  Pump,
              FlowSegment,
                  CableCarrierSegment,
                  CableSegment,
                  DuctSegment,
                  PipeSegment,
    FlowStorageDevice = 1300,
    ElectricFlowStorageDevice = 1301,
    Battery = 1302,
    Tank = 1310,
    FlowTerminal = 1400,
    AirTerminal = 1401,
    AudioVisualAppliance = 1410,
    Radio = 1411,
    Television = 1412,
    Speakers = 1413,
    CommunicationsAppliance = 1450,
    Telephone = 1451,
    ElectricAppliance = 1500,
    Range = 1501,
    Stove = 1502,
    Oven = 1503,
    CookTop = 1504,
    Refrigerator = 1505,
    Microwave = 1506,
    Dyer = 1507,
    FireSuppressionTerminal = 1600,
    LightFixture = 1610,
    Lamp = 1611,
    MedicalDevice = 1650,
    Outlet = 1700,
    SanitaryTerminal = 1720,
    Sink = 1721,
    Toilet = 1722,
    WashingMashine = 1723,
    Shower = 1724,
    Bath = 1725,
    Dishwasher = 1726,
    SpaceHeater = 1800,
                  StackTerminal,
                  WasteTerminal,
              FlowTreatmentDevice,
                  DuctSilencer,
                  Filter,
                  Interceptor,
}