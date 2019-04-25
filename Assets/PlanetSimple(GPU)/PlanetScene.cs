using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetScene : MonoBehaviour {

    public ComputeShader ComputeShader;
    public Shader VisualShader;

    public int Resolution;
    public Material PlanetMaterial;

    public Texture2D DyeTexture;
    public Texture2D Velocity;

    FluidSimulation FluidSimulation;
   
    // Use this for initialization
    void Start () {

        FluidSimulation= new FluidSimulation();
        FluidSimulation.ComputeShader = ComputeShader;

        FluidSimulation.setResolution(Resolution);

        FluidSimulation.SetDyeTexture(DyeTexture);
        FluidSimulation.setVelocity(Velocity);

        PlanetMaterial.SetTexture("_MainTex", FluidSimulation._colorRT1);

        Vector4[] aoKernel= new Vector4[16];
        for (int i = 0; i < 16; i++) {

            aoKernel[i] = new Vector4(Random.Range(0,2.0f)-1, Random.Range(0, 2.0f) - 1, 0,0);

        }

        PlanetMaterial.SetVectorArray("_AOKernel",aoKernel);


    }

    // Update is called once per frame
    void Update () {
    
        FluidSimulation.SimStep(Time.deltaTime);

        // Not sure if this is really necessary
        PlanetMaterial.SetTexture("_MainTex", FluidSimulation._colorRT1);
        PlanetMaterial.SetTexture("_Velo", FluidSimulation._velRT2);

        // PlanetRotation
        //transform.localEulerAngles+= new Vector3(0,1,0)*Time.deltaTime*30;


    }


}
