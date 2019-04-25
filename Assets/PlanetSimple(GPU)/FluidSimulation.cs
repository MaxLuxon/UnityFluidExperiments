using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FluidSimulation {

    // This simulation is based on following paper & code
    // http://www.dgp.toronto.edu/people/stam/reality/Research/pdf/GDC03.pdf
    // https://github.com/keijiro/StableFluids

    int Resolution;
    public float Viscosity = 12;

    [SerializeField] float _viscosity = 1e-6f;

    // Input Textures
    public Texture2D velocityTexture;
    public Texture2D dyeTexture;

    // Compute Shader Section
    public ComputeShader ComputeShader;

    // The Ids of the indviual subprograms of the compute shader 
    static class Kernels {

        public const int Advect = 0;
        public const int Project0 = 1;
        public const int Project1 = 2;
        public const int Diffuse0 = 3;
        public const int AddForce = 4;
        public const int AddDye = 5;
        public const int Project2 = 6;

    }

    int ThreadCountX { get { return (Resolution + 7) / 8; } }
    int ThreadCountY { get { return (Resolution * Screen.height / Screen.width + 7) / 8; } }
    int ResolutionX { get { return ThreadCountX * 8; } }
    int ResolutionY { get { return ThreadCountY * 8; } }

    public FluidSimulation() { }
    ~FluidSimulation() {}

    // Vector field buffers
    // This is where the simulation data is stored
    public static RenderTexture V_IN;
    public static RenderTexture V_OUT;

    public static RenderTexture D_IN;
    public static RenderTexture D_OUT;

    public static RenderTexture TEMP;
    public static RenderTexture Div;

    // Color buffers (for double buffering)
    // Visual images are saved here 
    // We use double buffering because the caculation is (probably) synchron and we want to avoid flickering
    public RenderTexture _colorRT1;
    public RenderTexture _colorRT2;

    // The velocity texture
    public RenderTexture _velRT2;
    public RenderTexture _velRT1;

    RenderTexture AllocateBuffer(int componentCount, int width = 0, int height = 0) {

        var format = RenderTextureFormat.ARGBHalf;
        if (componentCount == 1) format = RenderTextureFormat.RHalf;
        if (componentCount == 2) format = RenderTextureFormat.RGHalf;
        if (componentCount == 4) format = RenderTextureFormat.ARGBHalf;

        if (width == 0) width = ResolutionX;
        if (height == 0) height = ResolutionY;

        var rt = new RenderTexture(width, height, 0, format);
        rt.enableRandomWrite = true;
        rt.Create();
        return rt;

    }

    public void SetDyeTexture(Texture2D texture) {

        dyeTexture = texture;

    }

    public void setVelocity(Texture2D tex) {

        velocityTexture = tex;

    }

    public void setResolution(int resolution) {

        Resolution = resolution;

        V_IN = AllocateBuffer(4);
        V_OUT = AllocateBuffer(4);
        D_IN = AllocateBuffer(4);
        D_OUT = AllocateBuffer(4);

        Div = AllocateBuffer(4);

        _velRT2 = AllocateBuffer(4);
        _velRT1 = AllocateBuffer(4);

        _colorRT1 = AllocateBuffer(4, resolution, resolution);
        _colorRT2 = AllocateBuffer(4, resolution, resolution);

        V_IN.wrapMode = TextureWrapMode.Repeat;
        V_OUT.wrapMode = TextureWrapMode.Repeat;
        D_IN.wrapMode = TextureWrapMode.Repeat;
        D_OUT.wrapMode = TextureWrapMode.Repeat;
        Div.wrapMode = TextureWrapMode.Repeat;

        _velRT2.wrapMode = TextureWrapMode.Repeat;
        _velRT1.wrapMode = TextureWrapMode.Repeat;

        TEMP = AllocateBuffer(4);
        TEMP.wrapMode = TextureWrapMode.Repeat;

        _colorRT1.wrapMode = TextureWrapMode.Repeat;
        _colorRT2.wrapMode = TextureWrapMode.Repeat;


    }

    void swapBuffers(ref RenderTexture rt1, ref RenderTexture rt2) {

        RenderTexture temp = rt1;
        rt1 = rt2;
        rt2 = temp;

    }

    public void SimStep(float dt) {

        var dx = 1.0f / Resolution;

        // Common variables
        ComputeShader.SetFloat("DeltaTime", dt);
        ComputeShader.SetFloat("Time", Time.time);

        // Add Dye
        if (Input.GetKey(KeyCode.P)) {

            ComputeShader.SetTexture(Kernels.AddForce, "Map_In", dyeTexture);
            ComputeShader.SetTexture(Kernels.AddForce, "V4Field_out", V_IN);
            ComputeShader.SetTexture(Kernels.AddForce, "V4Field_in", V_IN);
            ComputeShader.Dispatch(Kernels.AddForce, ThreadCountX, ThreadCountY, 1);

        }

        // Add velocity
        if (Input.GetKey(KeyCode.L)) {

            ComputeShader.SetTexture(Kernels.AddDye, "Map_In", velocityTexture);
            ComputeShader.SetTexture(Kernels.AddDye, "V4Field_in", D_IN);
            ComputeShader.SetTexture(Kernels.AddDye, "V4Field_out", D_IN);
            ComputeShader.Dispatch(Kernels.AddDye, ThreadCountX, ThreadCountY, 1);

        }

        // Run Simulation
        if (Input.GetKey(KeyCode.O)) {

            Graphics.CopyTexture(V_IN, TEMP);
            var dif_alpha = dx * dx / (_viscosity * 0.001f * dt);
            ComputeShader.SetFloat("Alpha", dif_alpha);
            ComputeShader.SetFloat("Beta", 4 + dif_alpha);
            ComputeShader.SetTexture(Kernels.Diffuse0, "V4Field_Temp", TEMP);

            // Diffuse Velocity
            for (int i = 0; i < 10; i++) {

                ComputeShader.SetTexture(Kernels.Diffuse0, "V4Field_in", V_IN);
                ComputeShader.SetTexture(Kernels.Diffuse0, "V4Field_out", V_OUT);

                ComputeShader.Dispatch(Kernels.Diffuse0, ThreadCountX, ThreadCountY, 1);

                swapBuffers(ref V_IN, ref V_OUT);
            }

            //Advect Velocity
            ComputeShader.SetTexture(Kernels.Advect, "V4Field_out", V_OUT);
            ComputeShader.SetTexture(Kernels.Advect, "V4Field_Temp", V_IN);
            ComputeShader.SetTexture(Kernels.Advect, "Map_In", V_IN);
            ComputeShader.Dispatch(Kernels.Advect, ThreadCountX, ThreadCountY, 1);

            swapBuffers(ref V_IN, ref V_OUT);


            // Project Velocity
            // This means: make sure that water mass is neither destroyed nor created
            ComputeShader.SetTexture(Kernels.Project0, "V4Field_in", V_IN);
            ComputeShader.SetTexture(Kernels.Project0, "V4Field_Temp", TEMP);
            ComputeShader.SetTexture(Kernels.Project0, "Div", Div);
            ComputeShader.Dispatch(Kernels.Project0, ThreadCountX, ThreadCountY, 1);

            ComputeShader.SetTexture(Kernels.Project1, "Div", Div);

            for (int i = 0; i < 40; i++) {

                ComputeShader.SetTexture(Kernels.Project1, "V4Field_Temp", TEMP);
                ComputeShader.SetTexture(Kernels.Project1, "V4Field_out", V_OUT);
                ComputeShader.Dispatch(Kernels.Project1, ThreadCountX, ThreadCountY, 1);

                swapBuffers(ref TEMP, ref V_OUT);

            }

            ComputeShader.SetTexture(Kernels.Project2, "V4Field_Temp", TEMP);
            ComputeShader.SetTexture(Kernels.Project2, "V4Field_in", V_IN);
            ComputeShader.SetTexture(Kernels.Project2, "V4Field_out", V_IN);
            ComputeShader.Dispatch(Kernels.Project2, ThreadCountX, ThreadCountY, 1);

      

            // DYE
            Graphics.CopyTexture(D_IN, TEMP);
            ComputeShader.SetFloat("Alpha", dif_alpha * 0.0033f);
            ComputeShader.SetFloat("Beta", 4 + dif_alpha * 0.0033f);
            ComputeShader.SetTexture(Kernels.Diffuse0, "V4Field_Temp", TEMP);

            // Diffuse Dye
            for (int i = 0; i < 5; i++) {

                ComputeShader.SetTexture(Kernels.Diffuse0, "V4Field_in", D_IN);
                ComputeShader.SetTexture(Kernels.Diffuse0, "V4Field_out", D_OUT);
                ComputeShader.Dispatch(Kernels.Diffuse0, ThreadCountX, ThreadCountY, 1);

                swapBuffers(ref D_IN, ref D_OUT);

            }

            // Advect Dye
            ComputeShader.SetTexture(Kernels.Advect, "V4Field_Temp", V_IN);
            ComputeShader.SetTexture(Kernels.Advect, "Map_In", D_IN);
            ComputeShader.SetTexture(Kernels.Advect, "V4Field_out", D_OUT);
            ComputeShader.Dispatch(Kernels.Advect, ThreadCountX, ThreadCountY, 1);

            swapBuffers(ref D_IN, ref D_OUT);

        }

        // Copy buffer into end image
        Graphics.Blit(D_IN, _colorRT1);
        Graphics.Blit(V_IN, _velRT2);

        swapBuffers(ref _colorRT1, ref _colorRT2);
        swapBuffers(ref _velRT2, ref _velRT1);


    

    }


}
