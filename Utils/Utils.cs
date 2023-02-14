using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Windows;

namespace BimkravRvt.Utils
{
    class Utils
    {
#if (V2020||V2021||V2022)
        public static ParameterType ConvertFromIfcType(string type)
        {
            var dict = new Dictionary<string, ParameterType>()
            {
                {"Area", ParameterType.Area},
                {"Boolean", ParameterType.YesNo},
                {"ClassificationReference", ParameterType.Text},
                {"ColorTemperature", ParameterType.ColorTemperature},
                {"Count", ParameterType.Integer},
                {"Currency", ParameterType.Currency},
                {"ElectricalCurrent", ParameterType.ElectricalCurrent},
                {"ElectricalEfficiacy", ParameterType.ElectricalEfficacy},
                {"ElectricalVoltage", ParameterType.Number},
                {"Force", ParameterType.Force},
                {"Frequency", ParameterType.StructuralFrequency},
                {"Identifier", ParameterType.Text},
                {"Illuminance", ParameterType.ElectricalIlluminance},
                {"Integer", ParameterType.Integer},
                {"Label", ParameterType.Text},
                {"Length", ParameterType.Length},
                {"LinearVelocity", ParameterType.Speed},
                {"Logical", ParameterType.YesNo},
                {"LuminousFlux", ParameterType.ElectricalLuminousFlux},
                {"LuminousIntensity", ParameterType.ElectricalLuminousIntensity},
                {"NormalisedRatio", ParameterType.Number},
                {"PlaneAngle", ParameterType.Angle},
                {"PositiveLength", ParameterType.Length},
                {"PositivePlaneAngle", ParameterType.Angle},
                {"PositiveRatio", ParameterType.Number},
                {"Power", ParameterType.ElectricalPower},
                {"Pressure", ParameterType.PipingPressure},
                {"Ratio", ParameterType.Number},
                {"Real", ParameterType.Number},
                {"Text", ParameterType.Text},
                {"ThermalTransmittance", ParameterType.Number},
                {"ThermodynamicTemperature", ParameterType.PipingTemperature},
                {"Volume", ParameterType.Volume},
                {"VolumetricFlowRate", ParameterType.PipingFlow}
            };

            bool defined = dict.TryGetValue(type, out ParameterType parameterType);
            return defined ? parameterType : ParameterType.Text;
        }
#else
        public static ForgeTypeId ConvertFromIfcType(string type)
        {
            var dict = new Dictionary<string, ForgeTypeId>()
            {
                {"Area", SpecTypeId.Area},
                {"Boolean", SpecTypeId.Boolean.YesNo},
                {"ClassificationReference", SpecTypeId.String.Text},
                {"ColorTemperature", SpecTypeId.ColorTemperature},
                {"Count", SpecTypeId.Int.Integer},
                {"Currency", SpecTypeId.Currency},
                {"ElectricalCurrent", SpecTypeId.Current},
                {"ElectricalEfficiacy", SpecTypeId.Efficacy},
                {"ElectricalVoltage", SpecTypeId.Number},
                {"Force", SpecTypeId.Force},
                {"Frequency", SpecTypeId.StructuralFrequency},
                {"Identifier", SpecTypeId.String.Text},
                {"Illuminance", SpecTypeId.Illuminance},
                {"Integer", SpecTypeId.Int.Integer},
                {"Label", SpecTypeId.String.Text},
                {"Length", SpecTypeId.Length},
                {"LinearVelocity", SpecTypeId.Speed},
                {"Logical", SpecTypeId.Boolean.YesNo},
                {"LuminousFlux", SpecTypeId.LuminousFlux},
                {"LuminousIntensity", SpecTypeId.LuminousIntensity},
                {"NormalisedRatio", SpecTypeId.Number},
                {"PlaneAngle", SpecTypeId.Angle},
                {"PositiveLength", SpecTypeId.Length},
                {"PositivePlaneAngle", SpecTypeId.Angle},
                {"PositiveRatio", SpecTypeId.Number},
                {"Power", SpecTypeId.ElectricalPower},
                {"Pressure", SpecTypeId.PipingPressure},
                {"Ratio", SpecTypeId.Number},
                {"Real", SpecTypeId.Number},
                {"Text", SpecTypeId.String.Text},
                {"ThermalTransmittance", SpecTypeId.Number},
                {"ThermodynamicTemperature", SpecTypeId.PipingTemperature},
                {"Volume", SpecTypeId.Volume},
                {"VolumetricFlowRate", SpecTypeId.Flow}
            };

            bool defined = dict.TryGetValue(type, out ForgeTypeId parameterType);
            return defined ? parameterType : SpecTypeId.String.Text;
        }
#endif
        public static void CheckBounds(Window self)
        {
            if (self.Left > SystemParameters.PrimaryScreenWidth)
                self.Left = 200;
            if (self.Top > SystemParameters.PrimaryScreenHeight)
                self.Top = 200;
        }

        public static Guid GetIFCParameterGUID()
        {
            return new Guid("2909a7a7-be3f-40c1-afab-5c629662a0e1");
        }
        public static Guid GetSharedParameterGUID()
        {
            return new Guid("614fd0e5-de16-47f6-8eef-fc4da7c428af");
        }
        public static Guid GetProjectParameterGUID()
        {
            return new Guid("84d1d556-9739-4b83-92c6-d3399b130695");
        }
        public static Guid GetDisciplineParameterGUID()
        {
            return new Guid("490fba6c-495d-40ea-b097-3b7b822593fc");
        }
        public static Guid GetPhaseParameterGUID()
        {
            return new Guid("05485f6f-a442-49d0-b1ea-11b41926529c");
        }
        public static Guid GetUsernameParameterGUID()
        {
            return new Guid("8947b10d-6144-4f42-9dc2-c1a7cd4618e0");
        }
        public static Guid GetPasswordParameterGUID()
        {
            return new Guid("2c76ad1f-47bc-408a-827d-bfd0893d8e5b");
        }
        public static string SharedHeader
        {
            get
            {
                return "# This is a Revit shared parameter file.\r\n# Do not edit manually.\r\n*META\tVERSION\tMINVERSION\r\nMETA\t2\t1\r\n*GROUP\tID\tNAME\r\n*PARAM\tGUID\tNAME\tDATATYPE\tDATACATEGORY\tGROUP\tVISIBLE\tDESCRIPTION\tUSERMODIFIABLE\r\n";
            }
        }
        public static string IfcHeader
        {
            get
            {
                return "#\r\n# User Defined PropertySet Definition File\r\n#\r\n# Format:\r\n#PropertySet:\t<Pset Name>\tI[nstance]/T[ype]\t<element list separated by ','>\r\n#\t<Property Name 1>\t<Data type>\t<[opt] Revit parameter name, if different from IFC>\r\n#\t<Property Name 2>\t<Data type>\t<[opt] Revit parameter name, if different from IFC>\r\n#       ...\r\n#\r\n# Data types supported: Area, Boolean, ClassificationReference, ColorTemperature, Count, Currency,\r\n#\tElectricalCurrent, ElectricalEfficacy, ElectricalVoltage, Force, Frequency, Identifier,\r\n#\tIlluminance, Integer, Label, Length, Logical, LuminousFlux, LuminousIntensity,\r\n#\tNormalisedRatio, MassDensity, PlaneAngle, PositiveLength, PositivePlaneAngle, PositiveRatio,\r\n#\tPower, Pressure, Ratio, Real, Text, ThermalTransmittance, ThermodynamicTemperature, Volume,\r\n#\tVolumetricFlowRate\r\n#\r\n";
            }
        }

    }
}
