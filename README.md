Migrations.Json.Net
===================

A simple framework for data migrations using Newtonsoft Json.Net. o

Example From Our Production Code
================================

      using System;
      using System.Runtime.Serialization;
      using Newtonsoft.Json;
      using Newtonsoft.Json.Linq;
      using ReactiveUI.Ext;
      using Weingartner.Json.Migration;
      using Weingartner.Lens;
      
      namespace Weingartner.Numerics.Pump
      {
          [DataContract]
          [Migratable("-1067609732")]
          public class MoineauPostProcessingDocumentConfiguration : Immutable, IEquatable<MoineauPostProcessingDocumentConfiguration>
          {
              [DataMember]
              public Uri MoineauMachinePathSolverId { get; private set; }
      
              [DataMember]
              public Uri MachineDefinition { get; private set; }
      
              [DataMember]
              public string ProgramName { get; private set; }
      
              [DataMember]
              public double ContourLength { get; private set; }
      
              [DataMember]
              public double CuttingSpeed { get; private set; }
      
              [DataMember]
              public CuttingSpeedUnitsEnum DisplayCuttingSpeedUnit { get; private set; }
      
              [DataMember]
              public bool UseFeedCompensation { get; private set; }
      
              [DataMember]
              public double PeakChipFeed { get; private set; }
      
              [DataMember]
              public double InChipFeed { get; private set; }
      
              [DataMember]
              public double OutChipFeed { get; private set; }
      
              [DataMember]
              public double FeedCompensation { get; private set; }
      
              [DataMember]
              public double RunInLength { get; private set; }
      
              [DataMember]
              public double RunOutLength { get; private set; }
      
              [DataMember]
              public double ClampingLength { get; private set; }
      
              [DataMember]
              public double BarRadius { get; private set; }
      
              [DataMember]
              public double ZStep { get; private set; }
      
              [DataMember]
              public double RadialStepWidth { get; private set; }
      
              [DataMember]
              public double CutterChangeDistance { get; private set; }
      
              [DataMember] 
              public bool HelicalMilling { get; private set; }
      
              [DataMember]
              public bool InverseMilling { get; private set; }
      
              [DataMember]
              public bool SymmetricParabolicCorrection { get; private set; }
      
              [DataMember]
              public bool UseLeftParabolicCorrection { get; private set; }
      
              [DataMember]
              public bool UseHandwheel { get; private set; }
      
              [DataMember]
              public bool UseLaserMeasurement { get; private set; }
      
              [DataMember]
              public bool UseCustomPostTemplate { get; private set; }
      
              [DataMember]
              public string CustomPostTemplate { get; private set; }
      
              [DataMember]
              public ParabolicEndCorrectionConfiguration LeftParabolicCorrection { get; private set; }
      
              [DataMember]
              public bool SetSteadyPressureChecked { get; private set; }
      
              [DataMember]
              public int FrontSteadyPressureStage { get; private set; }
      
              [DataMember]
              public double FrontSteadyPressure { get; private set; }
      
              [DataMember]
              public int RearSteadyPressureStage { get; private set; }
      
              [DataMember]
              public double RearSteadyPressure { get; private set; }
      
              [DataMember]
              public string Comment { get; private set; }
      
              [DataMember]
              public bool UseConicCorrection { get; private set; }
      
              [DataMember]
              public double ConicCorrectionPeak { get; private set; }
      
              [DataMember]
              public double ConicCorrectionValley { get; private set; }
      
              [DataMember]
              public double CircularCorrection { get; private set; }
      
              [DataMember]
              public string PathToCNCProgram { get; private set; }
      
              public Maybe<ParabolicEndCorrectionConfiguration> LeftParabolicCorrectionMaybe => UseLeftParabolicCorrection ? LeftParabolicCorrection.ToMaybe() : None.Default;
      
              [DataMember]
              public bool UseRightParabolicCorrection{ get; private set; }
      
              [DataMember]
              public ParabolicEndCorrectionConfiguration RightParabolicCorrection { get; private set; }
              
              public Maybe<ParabolicEndCorrectionConfiguration> RightParabolicCorrectionMaybe => UseRightParabolicCorrection ? RightParabolicCorrection.ToMaybe() : None.Default;
      
              public double MaterialLength => ClampingLength + RunOutLength + ContourLength;
      
              public double StartPosition => MaterialLength + RunInLength;
      
              public enum EndCorrectionEnum
              {
                   None    
                 , Symmetric
                 , Asymmetric
              }
      
              private MoineauPostProcessingDocumentConfiguration()
              {
              }
      
              public static MoineauPostProcessingDocumentConfiguration Default =
                  new MoineauPostProcessingDocumentConfiguration
                  {
                      MoineauMachinePathSolverId = new Uri("wgc://"),
                      ProgramName = "Prog0",
      
                      ContourLength = 1, // 1 meter
                      CuttingSpeed = 150/60.0,// [ms-1]
      
                      PeakChipFeed = 0.3/1000, // m
                      InChipFeed = 0.2/1000, // m
                      OutChipFeed = 0.2/1000, // m
                      UseFeedCompensation = false,
                      FeedCompensation = 0.5, // [0-1] only active when UseFeedCompensation is true
      
                      RunInLength = 0.0, // m
                      RunOutLength = 0.0, // m
      
                      ClampingLength = 0.5, // m
      
                      BarRadius = 0.044, //m
      
                      ZStep = 0.002, //m
      
                      RadialStepWidth = 0.0, //m
      
                      CutterChangeDistance = 0.12, //m
      
                      HelicalMilling = true,
      
                      SymmetricParabolicCorrection = false,
                      InverseMilling = false,
      
                      UseLeftParabolicCorrection = false,
                      LeftParabolicCorrection = new ParabolicEndCorrectionConfiguration(),
      
                      UseRightParabolicCorrection = false,
                      RightParabolicCorrection = new ParabolicEndCorrectionConfiguration(),
                      UseHandwheel = false,
                      UseLaserMeasurement = false,
      
                      SetSteadyPressureChecked = false,
                      FrontSteadyPressureStage = 2,
                      RearSteadyPressureStage = 2,
                      FrontSteadyPressure = 10.0,
                      RearSteadyPressure = 10.0,
      
                      Comment = "",
      
                      UseConicCorrection = false,
                      ConicCorrectionPeak = 0,
                      ConicCorrectionValley = 0,
                      CircularCorrection = 0,
      
                      PathToCNCProgram = "",
                  };
      
      
              #region equality
              public bool Equals(MoineauPostProcessingDocumentConfiguration other)
              {
                  if (ReferenceEquals( null, other )) return false;
                  if (ReferenceEquals( this, other )) return true;
                  return Equals( MoineauMachinePathSolverId, other.MoineauMachinePathSolverId ) && Equals( MachineDefinition, other.MachineDefinition ) && string.Equals( ProgramName, other.ProgramName ) && ContourLength.Equals( other.ContourLength ) && CuttingSpeed.Equals( other.CuttingSpeed ) && DisplayCuttingSpeedUnit == other.DisplayCuttingSpeedUnit && UseFeedCompensation == other.UseFeedCompensation && PeakChipFeed.Equals( other.PeakChipFeed ) && InChipFeed.Equals( other.InChipFeed ) && OutChipFeed.Equals( other.OutChipFeed ) && FeedCompensation.Equals( other.FeedCompensation ) && RunInLength.Equals( other.RunInLength ) && RunOutLength.Equals( other.RunOutLength ) && ClampingLength.Equals( other.ClampingLength ) && BarRadius.Equals( other.BarRadius ) && ZStep.Equals( other.ZStep ) && RadialStepWidth.Equals( other.RadialStepWidth ) && CutterChangeDistance.Equals( other.CutterChangeDistance ) && HelicalMilling == other.HelicalMilling && InverseMilling == other.InverseMilling && SymmetricParabolicCorrection == other.SymmetricParabolicCorrection && UseLeftParabolicCorrection == other.UseLeftParabolicCorrection && UseHandwheel == other.UseHandwheel && UseLaserMeasurement == other.UseLaserMeasurement && UseCustomPostTemplate == other.UseCustomPostTemplate && string.Equals( CustomPostTemplate, other.CustomPostTemplate ) && Equals( LeftParabolicCorrection, other.LeftParabolicCorrection ) && SetSteadyPressureChecked == other.SetSteadyPressureChecked && FrontSteadyPressureStage == other.FrontSteadyPressureStage && FrontSteadyPressure.Equals( other.FrontSteadyPressure ) && RearSteadyPressureStage == other.RearSteadyPressureStage && RearSteadyPressure.Equals( other.RearSteadyPressure ) && string.Equals( Comment, other.Comment ) && UseConicCorrection == other.UseConicCorrection && ConicCorrectionPeak.Equals( other.ConicCorrectionPeak ) && ConicCorrectionValley.Equals( other.ConicCorrectionValley ) && CircularCorrection.Equals( other.CircularCorrection ) && string.Equals( PathToCNCProgram, other.PathToCNCProgram ) && UseRightParabolicCorrection == other.UseRightParabolicCorrection && Equals( RightParabolicCorrection, other.RightParabolicCorrection );
              }
      
              public override bool Equals(object obj)
              {
                  if (ReferenceEquals( null, obj )) return false;
                  if (ReferenceEquals( this, obj )) return true;
                  if (obj.GetType() != this.GetType()) return false;
                  return Equals( (MoineauPostProcessingDocumentConfiguration) obj );
              }
      
              public override int GetHashCode()
              {
                  unchecked
                  {
                      var hashCode = (MoineauMachinePathSolverId != null ? MoineauMachinePathSolverId.GetHashCode() : 0);
                      hashCode = (hashCode*397) ^ (MachineDefinition != null ? MachineDefinition.GetHashCode() : 0);
                      hashCode = (hashCode*397) ^ (ProgramName != null ? ProgramName.GetHashCode() : 0);
                      hashCode = (hashCode*397) ^ ContourLength.GetHashCode();
                      hashCode = (hashCode*397) ^ CuttingSpeed.GetHashCode();
                      hashCode = (hashCode*397) ^ (int) DisplayCuttingSpeedUnit;
                      hashCode = (hashCode*397) ^ UseFeedCompensation.GetHashCode();
                      hashCode = (hashCode*397) ^ PeakChipFeed.GetHashCode();
                      hashCode = (hashCode*397) ^ InChipFeed.GetHashCode();
                      hashCode = (hashCode*397) ^ OutChipFeed.GetHashCode();
                      hashCode = (hashCode*397) ^ FeedCompensation.GetHashCode();
                      hashCode = (hashCode*397) ^ RunInLength.GetHashCode();
                      hashCode = (hashCode*397) ^ RunOutLength.GetHashCode();
                      hashCode = (hashCode*397) ^ ClampingLength.GetHashCode();
                      hashCode = (hashCode*397) ^ BarRadius.GetHashCode();
                      hashCode = (hashCode*397) ^ ZStep.GetHashCode();
                      hashCode = (hashCode*397) ^ RadialStepWidth.GetHashCode();
                      hashCode = (hashCode*397) ^ CutterChangeDistance.GetHashCode();
                      hashCode = (hashCode*397) ^ HelicalMilling.GetHashCode();
                      hashCode = (hashCode*397) ^ InverseMilling.GetHashCode();
                      hashCode = (hashCode*397) ^ SymmetricParabolicCorrection.GetHashCode();
                      hashCode = (hashCode*397) ^ UseLeftParabolicCorrection.GetHashCode();
                      hashCode = (hashCode*397) ^ UseHandwheel.GetHashCode();
                      hashCode = (hashCode*397) ^ UseLaserMeasurement.GetHashCode();
                      hashCode = (hashCode*397) ^ UseCustomPostTemplate.GetHashCode();
                      hashCode = (hashCode*397) ^ (CustomPostTemplate != null ? CustomPostTemplate.GetHashCode() : 0);
                      hashCode = (hashCode*397) ^ (LeftParabolicCorrection != null ? LeftParabolicCorrection.GetHashCode() : 0);
                      hashCode = (hashCode*397) ^ SetSteadyPressureChecked.GetHashCode();
                      hashCode = (hashCode*397) ^ FrontSteadyPressureStage;
                      hashCode = (hashCode*397) ^ FrontSteadyPressure.GetHashCode();
                      hashCode = (hashCode*397) ^ RearSteadyPressureStage;
                      hashCode = (hashCode*397) ^ RearSteadyPressure.GetHashCode();
                      hashCode = (hashCode*397) ^ (Comment != null ? Comment.GetHashCode() : 0);
                      hashCode = (hashCode*397) ^ UseConicCorrection.GetHashCode();
                      hashCode = (hashCode*397) ^ ConicCorrectionPeak.GetHashCode();
                      hashCode = (hashCode*397) ^ ConicCorrectionValley.GetHashCode();
                      hashCode = (hashCode*397) ^ CircularCorrection.GetHashCode();
                      hashCode = (hashCode*397) ^ (PathToCNCProgram != null ? PathToCNCProgram.GetHashCode() : 0);
                      hashCode = (hashCode*397) ^ UseRightParabolicCorrection.GetHashCode();
                      hashCode = (hashCode*397) ^ (RightParabolicCorrection != null ? RightParabolicCorrection.GetHashCode() : 0);
                      return hashCode;
                  }
              }
      
              public static bool operator ==(MoineauPostProcessingDocumentConfiguration left, MoineauPostProcessingDocumentConfiguration right)
              {
                  return Equals(left, right);
              }
      
              public static bool operator !=(MoineauPostProcessingDocumentConfiguration left, MoineauPostProcessingDocumentConfiguration right)
              {
                  return !Equals(left, right);
              }
              #endregion
      
              #region Migration
              // ReSharper disable UnusedMember.Local
      
              private static JObject Migrate_1(JObject data, JsonSerializer serializer)
              {
                  data["MachineDefinitionName"] = "";
                  return data;
              }
      
              private static JObject Migrate_2(JObject data, JsonSerializer serializer)
              {
                  data["LeftParabolicCorrection"] = new ParabolicEndCorrectionConfiguration().ToJObject(serializer);
                  data["RightParabolicCorrection"] = new ParabolicEndCorrectionConfiguration().ToJObject(serializer);
                  data["UseLeftParabolicCorrection"] = false;
                  data["UseRightParabolicCorrection"] = false;
                  return data;
              }
      
              private static JObject Migrate_3(JObject data, JsonSerializer serializer)
              {
                  data["UseHandwheel"] = false;
                  data["UseCustomPostTemplate"] = false;
                  data["CustomPostTemplate"] = "";
                  return data;
              }
      
              private static JObject Migrate_4(JObject data, JsonSerializer serializer)
              {
                  data["SetSteadyPressureChecked"] = false;
                  data["FrontSteadyPressureStage"] = 2;
                  data["RearSteadyPressureStage"] = 2;
                  data["FrontSteadyPressure"] = 10.0;
                  data["RearSteadyPressure"] = 10.0;
                  return data;
              }
      
              private static JObject Migrate_5(JObject data, JsonSerializer serializer)
              {
                  data["UseMajorAsBarRadius"] = true;
                  return data;
              }
      
              private static JObject Migrate_6(JObject data, JsonSerializer serializer)
              {
                  data["Comment"] = "";
                  data["ConicCorrectionPeak"] = 0;
                  data["ConicCorrectionValley"] = 0;
                  data["CircularCorrection"] = 0;
                  data["UseConicCorrection"] = false;
                  return data;
              }
      
              private static JObject Migrate_7(JObject data, JsonSerializer serializer)
              {
                  data["MoineauMachinePathSolverId"] = "wgc://" + (data["MoineauMachinePathSolverId"] ?? "");
                  return data;
              }
      
              private static JObject Migrate_8(JObject data, JsonSerializer serializer)
              {
                  data.Remove("UseMajorAsBarRadius");
                  return data;
              }
      
              private static JObject Migrate_9(JObject data, JsonSerializer serializer)
              {
                  data["MachineDefinition"] = "";
                  data.Remove("MachineDefinitionName");
                  return data;
              }
      
              private static JObject Migrate_10(JObject data, JsonSerializer serializer)
              {
                  data["UseLaserMeasurement"] = false;
                  data["PathToCNCProgram"] = "";
                  data["PathToMdfFile"] = "";
                  data["CircularCorrection"] = (double)data["CircularCorrection"];
                  return data;
              }
      
              private static JObject Migrate_11(JObject data, JsonSerializer serializer)
              {
                  data["DisplayCuttingSpeedUnit"] = CuttingSpeedUnitsEnum.MmSec.ToString();
                  return data;
              }
      
              private static JObject Migrate_12(JObject data, JsonSerializer serializer)
              {
                  data["SymmetricParabolicCorrection"] = false;
                  return data;
              }
      
              private static JToken Migrate_13(JObject data, JsonSerializer serializer)
              {
                  data.Remove("PathToMdfFile");
                  return data;
              }
              // ReSharper restore UnusedMember.Local
              #endregion
        }
    }
