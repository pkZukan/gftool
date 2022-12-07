using FlatSharp.Attributes;

namespace GFTool.Core.Flatbuffers.TR.Scene
{
    [FlatBufferStruct]
    public struct Vec3f
    {
        public float x;
        public float y;
        public float z;
    };

    [FlatBufferTable]
    public class SRT
    {
        [FlatBufferItem(0)]
        public Vec3f scale { get; set; }

        [FlatBufferItem(1)]
        public Vec3f rotation { get; set; }

        [FlatBufferItem(2)]
        public Vec3f translation { get; set; }
    }

    

    [FlatBufferTable]
    public class ti_CaptureComponent
    {
    }

    [FlatBufferTable]
    public class ti_DynamicExclusionComponent
    {
        [FlatBufferItem(0)]
        public byte unk_0 { get; set; }
    }

    [FlatBufferTable]
    public class ti_EnvSoundComponent
    {
    }

    [FlatBufferTable]
    public class ti_FieldAttributePickerComponent
    {
        [FlatBufferItem(0)]
        public uint unk_0 { get; set; }

        [FlatBufferItem(1)]
        public uint unk_1 { get; set; }

        [FlatBufferItem(2)]
        public float unk_2 { get; set; }
    }

    [FlatBufferTable]
    public class ti_FieldPokemonComponent
    {
    }

    [FlatBufferTable]
    public class ti_ModelDitherFadeComponent
    {
        [FlatBufferItem(0)]
        public uint unk_0 { get; set; }

        [FlatBufferItem(1)]
        public float unk_1 { get; set; }

        [FlatBufferItem(2)]
        public float unk_2 { get; set; }

        [FlatBufferItem(3)]
        public float unk_3 { get; set; }

        [FlatBufferItem(4)]
        public float unk_4 { get; set; }
    }

    [FlatBufferTable]
    public class ti_PokemonModelComponent
    {
    }

    [FlatBufferTable]
    public class ti_PokemonModelComponentForSimpleParam
    {
    }

    [FlatBufferTable]
    public class ti_pokemonObject
    {
    }

    [FlatBufferTable]
    public class ti_PokeVoiceComponent
    {
    }

    [FlatBufferTable]
    public class TrinityAnimationComponent
    {
    }

    [FlatBufferTable]
    public class TrinityAtmosphereComponent
    {
    }

    [FlatBufferTable]
    public class TrinityAttachmentComponent
    {
    }

    [FlatBufferTable]
    public class TrinityCameraAnimationComponent
    {
    }

    [FlatBufferTable]
    public class TrinityCharacterCreationMasterComponent
    {
    }

    [FlatBufferTable]
    public class TrinityCollisionComponent
    {
        [FlatBufferItem(0)]
        public byte collision_type { get; set; }

        [FlatBufferItem(1)]
        public uint collision { get; set; }
    }

    [FlatBufferTable]
    public class TrinityCollisionEventTriggerComponent
    {
    }

    [FlatBufferTable]
    public class TrinityCombineLODEntity
    {
    }

    [FlatBufferTable]
    public class TrinityCombineObjectGroup
    {
    }

    [FlatBufferTable]
    public class TrinityCompoundBoxShape
    {
    }

    [FlatBufferTable]
    public class TrinityCompoundCapsuleShape
    {
    }

    [FlatBufferTable]
    public class TrinityCompoundPencilShape
    {
    }

    [FlatBufferTable]
    public class TrinityCompoundSphereShape
    {
    }

    [FlatBufferTable]
    public class TrinityConditionalStreamingArea
    {
    }

    [FlatBufferTable]
    public class TrinityConditionalStreamingConstraint
    {
    }

    [FlatBufferTable]
    public class TrinityConditionalStreamingGroup
    {
    }

    [FlatBufferTable]
    public class TrinityConditionalStreamingReset
    {
    }

    [FlatBufferTable]
    public class Trinityconfig
    {
    }

    [FlatBufferTable]
    public class TrinityDecalComponent
    {
    }

    [FlatBufferTable]
    public class TrinityEventStateEventTriggerComponent
    {
    }

    [FlatBufferTable]
    public class TrinityGridStremingGroup
    {
    }

    [FlatBufferTable]
    public class TrinityJointPrioritySetting
    {
    }

    [FlatBufferTable]
    public class TrinityLayoutCommonResourceComponent
    {
    }

    [FlatBufferTable]
    public class TrinityLayoutComponent
    {
    }

    [FlatBufferTable]
    public class TrinityLightApplierComponent
    {
    }

    [FlatBufferTable]
    public class TrinityLightDirectApplierComponent
    {
    }

    [FlatBufferTable]
    public class TrinityLocator
    {
    }

    [FlatBufferTable]
    public class TrinityModelComponent
    {
    }

    [FlatBufferTable]
    public class TrinityModelCullingSetting
    {
    }

    [FlatBufferTable]
    public class TrinityModelInstancerComponent
    {
    }

    [FlatBufferTable]
    public class TrinityModelLodSetting
    {
    }

    [FlatBufferTable]
    public class TrinityNavigatorComponent
    {
    }

    [FlatBufferTable]
    public class TrinityNavmeshComponent
    {
    }

    [FlatBufferTable]
    public class TrinityObjectSwitcher
    {
    }

    [FlatBufferTable]
    public class TrinityOverrideSensorData
    {
    }

    [FlatBufferTable]
    public class TrinityProgressEventTriggerComponent
    {
    }

    [FlatBufferTable]
    public class TrinitySceneController
    {
    }

    [FlatBufferTable]
    public class TrinitySceneObjectReference
    {
    }

    [FlatBufferTable]
    public class TrinityScenePoint
    {
        [FlatBufferItem(0)]
        public string point_name { get; set; }

        [FlatBufferItem(1)]
        public Vec3f point_location { get; set; }

        [FlatBufferItem(2)]
        public byte point_unk { get; set; }
    }

    [FlatBufferTable]
    public class TrinityStreamingPoint
    {
    }

    [FlatBufferTable]
    public class TrinityTerrainCollision
    {
    }

    [FlatBufferTable]
    public class TrinityTerrainEntity
    {
    }

    [FlatBufferTable]
    public class TrinityTerrainStreamingSetting
    {
    }

    [FlatBufferTable]
    public class TrinityTerrainTreeTemplate
    {
    }

    [FlatBufferTable]
    public class TrinityTerrainTreeTemplateRoot
    {
    }

    [FlatBufferTable]
    public class TrinityTextureBufferComponent
    {
    }

    [FlatBufferTable]
    public class TrinityVATComponent
    {
    }


    [FlatBufferTable]
    public class TrinityObjectTemplate
    {
        [FlatBufferItem(0)]
        public string ObjectName { get; set; }

        [FlatBufferItem(1)]
        public string TemplateName { get; set; }

        [FlatBufferItem(2)]
        public string FilePath { get; set; }

        [FlatBufferItem(3)]
        public byte unk_3 { get; set; }

        [FlatBufferItem(4)]
        public string ObjectType { get; set; }

        [FlatBufferItem(5)]
        public byte[] ObjectBytes { get; set; }
    }

    [FlatBufferTable]
    public class SceneEntry
    {
        [FlatBufferItem(0)]
        public string TypeName { get; set; }

        [FlatBufferItem(1)]
        public byte[] NestedType { get; set; }

        [FlatBufferItem(2)]
        public SceneEntry[] SubObjects { get; set; }
    }

    [FlatBufferTable]
    public class TrinitySceneObjTemplate
    {
        [FlatBufferItem(0)]
        public string SceneName { get; set; }

        [FlatBufferItem(1)]
        public string SceneExtra { get; set; }

        [FlatBufferItem(2)]
        public uint res_2 { get; set; }

        [FlatBufferItem(3)]
        public uint res_3 { get; set; }

        [FlatBufferItem(4)]
        public SceneEntry[] SceneObjectList { get; set; }

        [FlatBufferItem(5)]
        public uint res_4 { get; set; }

        [FlatBufferItem(6)]
        public uint unk_5 { get; set; }
    }
}
