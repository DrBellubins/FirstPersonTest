using Godot.NativeInterop;
using static Godot.GD;
using static Godot.RenderingDevice;

using Godot;
using Godot.Collections;
using System.Linq;
using System;

namespace Godot.Demo.CompositeEffect;

/// <summary>
/// This is a very simple effects demo that takes our color values and writes
/// back gray scale values. 
/// </summary>
/// <remarks>
/// This class is ported from GDScript into C#, you can check original source code below :
/// https://github.com/godotengine/godot-demo-projects/pull/942/commits/87b88e9e210a28793eb1644ad5cfd03704eba58f#diff-8f522b04134aba77317274c08729139f14fb5c7ae7a1b2468effce62ff134264 <para/>
/// Tested on 55730f9782 (PR : https://github.com/godotengine/godot/pull/80214) <para/>
/// Ported by Creta Park
/// </remarks>
[Tool]
[GlobalClass]
public partial class TonemapperCompositor : CompositorEffect
{
    private const string SHADER_RES_PATH = "res://Shaders/Tonemapper/AGX_Compute.glsl";
    
    public TonemapperCompositor() : base()
    {
        RenderingServer.CallOnRenderThread(new Callable(this, MethodName.InitializeCompute));
    }

    public override void _Notification(int what)
    {
        switch ((long)what)
        {
            case NotificationPredelete:
                //When this is called it should be safe to clean up our shader.
                //If not we'll crash anyway because we can no longer call our _render_callback.
                if (_shader.IsValid)
                    _renderingDevice.FreeRid(_shader);
                
                break;
        }
    }
    
    ///////////////////////////////////////////////////////////////////////////////
    //Everything after this point is designed to run on our rendering thread
    
    private RenderingDevice _renderingDevice;
    
    private Rid _shader;
    private Rid _pipeline;
    
    private void InitializeCompute()
    {
        _renderingDevice = RenderingServer.GetRenderingDevice();
        
        if (!IsInstanceValid(_renderingDevice))
            return;
        
        //Create our shader
        var shaderFile = Load<RDShaderFile>(SHADER_RES_PATH);
        var shaderSpirV = shaderFile.GetSpirV();
        
        _shader   = _renderingDevice.ShaderCreateFromSpirV(shaderSpirV);
        _pipeline = _renderingDevice.ComputePipelineCreate(_shader);
    }

    public override void _RenderCallback(int effectCallbackType, RenderData renderData)
    {
        if (!IsInstanceValid(_renderingDevice))
            return;

        //Get our render scene buffers object, this gives us access to our render buffers.
        //Note that implementation differs per renderer hence the need for the cast.
        RenderSceneBuffersRD renderSceneBuffers = renderData.GetRenderSceneBuffers()
            as RenderSceneBuffersRD;

        if (!IsInstanceValid(renderSceneBuffers))
            return;

        //Get our render size, this is the 3D render resolution!
        Vector2I size = renderSceneBuffers.GetInternalSize();

        if (size == Vector2I.Zero)
            return;

        //We can use a compute shader here
        uint xGroups = ((uint)size.X - 1) / 8 + 1;
        uint yGroups = ((uint)size.Y - 1) / 8 + 1;

        var packArray = new float[] { size.X, size.Y, 0.0f, 0.0f };

        // create a byte array and copy the floats into it...
        var packByteArray = new byte[packArray.Length * 4];
        Buffer.BlockCopy(packArray, 0, packByteArray, 0, packByteArray.Length);

        //Loop through views just in case we're doing stereo rendering. No extra cost if this is mono.
        uint viewCount = renderSceneBuffers.GetViewCount();

        for (uint view = 0; view < viewCount; view++)
        {
            //Get the RID for our color image, we will be reading from and writing to it.
            Rid inputImage = renderSceneBuffers.GetColorLayer(view);

            //Create a uniform set, this will be cached, the cache will be cleared if our viewports configuration is changed
            var uniform = new RDUniform()
            {
                UniformType = UniformType.Image,
                Binding = 0
            };

            uniform.AddId(inputImage);

            Rid uniformSet = UniformSetCacheRD.GetCache(_shader, 0, new() { uniform });

            //Run our compute shader
            var computeList = _renderingDevice.ComputeListBegin();
            {
                _renderingDevice.ComputeListBindComputePipeline(computeList, _pipeline);
                _renderingDevice.ComputeListBindUniformSet(computeList, uniformSet, setIndex: 0);
                _renderingDevice.ComputeListSetPushConstant(computeList, packByteArray, (uint)packByteArray.Length);
                _renderingDevice.ComputeListDispatch(computeList, xGroups, yGroups, 1);
            }

            _renderingDevice.ComputeListEnd();
        }
    }
}