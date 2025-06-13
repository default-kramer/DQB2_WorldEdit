using System;
using EyeOfRubiss.Info;
using EyeOfRubiss.Resources;
using Godot;

namespace EyeOfRubiss.Nodes
{
    public partial class PropMeshInstance : MeshInstance3D
    {
        public PropMeshInstance() {}

        public void Setup(StageData.Prop prop)
        {
			StageData.Chunk chunk = StageData.Instance.GetChunkAtIndex(prop.ChunkIndex);
            
			Position = new Vector3(prop.X, prop.Y, prop.Z) + chunk.GetOrigin();// + new Vector3(1024, 0, 1024);
			Rotation = new Vector3(0, (float)(prop.Rotation * Math.PI / 2), 0);
			VisibilityRangeEnd = 128;

            Mesh = ResourceLoader.Load<Mesh>("res://Resources/DUMMYOBJECTMESH.tres");

            PropInfo propInfo = PropInfo.Get(prop.PropID);

            if (propInfo is null)
                return;

            //GD.Print($"Created prop {propInfo.Name} ({propInfo.ID}) at ({Position}).");

            if (string.IsNullOrEmpty(propInfo.Mesh))
                return;
            
            PropMeshResource propMeshResource = ResourceLoader.Load<PropMeshResource>(propInfo.Mesh);
            if (propMeshResource is null)
            {
                GD.Print($"Could not find mesh resource. {propInfo.Name} ({propInfo.ID}) at {Position}");
                return;
            }

            if (propMeshResource.GetHighestResMesh() is Mesh mesh)
                Mesh = mesh;
            if (propMeshResource.GetHighestResMaterial() is BaseMaterial3D material)
                MaterialOverride = material;
        }
    }
}