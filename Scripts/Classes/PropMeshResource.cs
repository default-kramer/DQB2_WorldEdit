using System;
using Godot;

namespace EyeOfRubiss.Resources
{
    /// <summary> Informational resource for data pertaining to the rendering of Props. </summary>
    [GlobalClass]
    public partial class PropMeshResource : Resource
    {
        /// <summary> The highest level of detail mesh for this Prop. </summary>
        [Export] public Mesh Mesh_HighRes { get; set; }
        /// <summary> The median level of detail mesh for this Prop. </summary>
        [Export] public Mesh Mesh_MedRes { get; set; }
        /// <summary> The lowest level of detail mesh for this Prop. </summary>
        [Export] public Mesh Mesh_LowRes { get; set; }
        /// <summary> The material to use for the Prop's mesh. </summary>
        [Export] public BaseMaterial3D Material { get; set; }
        /// <summary> The billboard texture to use for this Prop's lowest-LOD representation. Automatically used instead of Material at low LOD if not null. </summary>
        [Export] public BaseMaterial3D Billboard { get; set; } 
        /// <summary> Additional offset for the model's transform. </summary>
        [Export] public Vector3 Offset { get; set; }

        public PropMeshResource(){}

        /// <summary> Returns the mesh for this Prop, at highest possible resolution. </summary>
        /// <returns> Mesh_HighRes if it exists; otherwise, Mesh_MedRes if it exists; otherwise, Mesh_LowRes if it exists; otherwise, null. </returns>
        public Mesh GetHighestResMesh()
        {
            if (Mesh_HighRes is not null)
                return Mesh_HighRes;
            if (Mesh_MedRes is not null)
                return Mesh_MedRes;
            if (Mesh_LowRes is not null)
                return Mesh_LowRes;
            else return null;
        }
        /// <summary> Returns the material for this Prop, at highest possible resolution. </summary>
        /// <returns> Billboard if Mesh_LowRes is the highest res mesh and Billboard is not null; otherwise, Material. </returns>
        public BaseMaterial3D GetHighestResMaterial()
        {
            if (Billboard is not null && Mesh_HighRes is null && Mesh_MedRes is null && Mesh_LowRes is not null)
                return Billboard;
            else if (Material is not null)
                return Material;
            else return null;
        }
    }
}