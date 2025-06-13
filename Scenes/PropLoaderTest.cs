using Godot;
using System;
using System.Threading;

public partial class PropLoaderTest : Node3D
{
    private Thread _TestThread;
    private MultiMeshInstance3D multiMeshInstance3D;

    public override void _Ready()
    {
        _OnReadyVariables();
    }
    private void _OnReadyVariables()
    {
        multiMeshInstance3D = GetNode<MultiMeshInstance3D>("MultiMeshInstance3D");
    }

    public void _OnButtonPressed()
    {
        _TestThread = new Thread(new ThreadStart(CreatePropsUsingMultiMesh));
        _TestThread.Start();
    }

    public void ThreadTestMethod()
    {
        GD.Print("Test thread Executed.");
        Thread.Sleep(100);
        GD.Print("Waiting.");
        Thread.Sleep(1000);
        GD.Print("5...");
        Thread.Sleep(1000);
        GD.Print("4...");
        Thread.Sleep(1000);
        GD.Print("3...");
        Thread.Sleep(1000);
        GD.Print("2...");
        Thread.Sleep(1000);
        GD.Print("1...");
        Thread.Sleep(1000);
        GD.Print("Test thread Ended.");
    }
    public void CreateProps()
    {
        Node dummyNode = new Node();
        Mesh mesh = ResourceLoader.Load<Mesh>("res://Mesh/unknown.obj");
        for (int i = 0; i < 0x8192; i++)
        {
            MeshInstance3D meshInstance3D = new MeshInstance3D();
            meshInstance3D.Mesh = mesh;
            meshInstance3D.Position = Vector3.Right * i;
            //meshInstance3D.Position = Vector3.Up * 0.5f;

            dummyNode.AddChild(meshInstance3D);
            GD.Print($"{i + 1} of 8192 objects created.");
        }
        CallDeferred("add_child", dummyNode);
        GD.Print("Thread complete.");

    }
    public void CreatePropsUsingMultiMesh()
    {
        MultiMesh multiMesh = multiMeshInstance3D.Multimesh;
        multiMesh.InstanceCount = 8192;
        for (int i = 0; i < multiMesh.InstanceCount; i++)
        {
            multiMesh.SetInstanceTransform(i, new Transform3D(Basis.Identity, Vector3.Right * i));
        }
        for (int i = 0; i < 100; i++)
        {
            MultiMeshInstance3D newMultiMeshInstance3D = new MultiMeshInstance3D();
            newMultiMeshInstance3D.Position += Vector3.Forward * i;
            newMultiMeshInstance3D.Multimesh = multiMesh;
            CallDeferred("add_child", newMultiMeshInstance3D);
        }

        GD.Print("Done");
    }
}
