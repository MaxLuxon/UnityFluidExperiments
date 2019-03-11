using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetScene : MonoBehaviour {

    public Material DisplayMaterial;
    public int SimulationResolution=30;

    FluidSimulation FluidSimulation;

    Texture2D Texture;
    public Texture2D TextureMask;
    public List<Vector4> tempDensities= new List<Vector4>();

    public Color BackgroundColor;
    public Color Color1;
    public Color Color2;
    public Color Color3;

    float nextDrop=0;
    bool sim=false;

    // Use this for initialization
    void Start () {

        FluidSimulation= new FluidSimulation();
        FluidSimulation.setResolution(SimulationResolution);
        FluidSimulation.diffusionRate=0.0001f;
        if(TextureMask!=null) FluidSimulation.CreateMaskFromTexture(TextureMask);

        Texture = new Texture2D(SimulationResolution, SimulationResolution);
        Texture.filterMode = FilterMode.Bilinear;
        Texture.wrapMode= TextureWrapMode.Clamp;

        DisplayMaterial.SetTexture("_MainTex", Texture);

        for (int i = 0; i < FluidSimulation.DensityArray.Count; i++) {
            tempDensities.Add(Vector4.zero);
        }

        }

        void UpdateTexture(){

        List<Color> color = new List<Color>();
        for (int i = 0; i < FluidSimulation.DensityArray.Count; i++) {

            Vector4 density= tempDensities[i]+ FluidSimulation.DensityArray[i];

            Color c = Color.Lerp(BackgroundColor, Color1, density.x );
            c = Color.Lerp(c, Color2, density.y);
            c = Color.Lerp(c,  Color3, density.z);


            color.Add(c);

        }

        Texture.SetPixels(0, 0, SimulationResolution, SimulationResolution, color.ToArray());
        Texture.Apply();

    }

    void TakeSnapShot(){

        for (int x = 0; x < SimulationResolution; x++) {
            for (int y = 0; y < SimulationResolution; y++) {

                int index= FluidSimulation.Ix(x, y);

                float xn= x+ FluidSimulation.VelocityArray[index].x*0.2f;
                float yn = y + FluidSimulation.VelocityArray[index].y * 0.2f;

                xn= Mathf.Clamp(xn,0,SimulationResolution-1);
                yn = Mathf.Clamp(yn, 0, SimulationResolution-1);

                int indexNew = FluidSimulation.Ix((int)xn, (int)yn);

                tempDensities[indexNew] -= tempDensities[indexNew]*Time.deltaTime*0.1f;
                tempDensities[indexNew] += FluidSimulation.DensityArray[index] * (FluidSimulation.Mask[index].x * 0.5f+0.5f)*0.3f * Time.deltaTime;


                // FluidSimulation.DensityArray[FluidSimulation.Ix(x,y)]-= new Vector4(1,1,1,1)*(1-FluidSimulation.Mask[FluidSimulation.Ix(x,y)].x)*Time.deltaTime*0.1f;
                // FluidSimulation.VelocityArray[FluidSimulation.Ix(x, y)] +=  new Vector2(FluidSimulation.Mask[FluidSimulation.Ix(x, y)].y, FluidSimulation.Mask[FluidSimulation.Ix(x, y)].z) * Time.deltaTime*0.1f;

            }

        }

        for (int i = 0; i < FluidSimulation.DensityArray.Count; i++) {

        }

    }

    // Update is called once per frame
    void Update () {

        if(Input.GetKeyDown(KeyCode.P)){

            sim=!sim;

        }

       if(sim) {

            nextDrop= Time.time+Random.Range(0,2f);

            int rx= (int)(SimulationResolution*0.5f);
            int ry = (int)(SimulationResolution * 0.5f);

            FluidSimulation.DensityArray[FluidSimulation.Ix(rx,ry)] += 40 * new Vector4(1, 0, 0, 0)*Time.deltaTime;
            FluidSimulation.VelocityArray[FluidSimulation.Ix(rx, ry)] +=  (Vector2.right+new Vector2(0,Mathf.Sin(Time.time))) * 200 * Time.deltaTime;

            int rx2 = (int)(SimulationResolution * 0.5f);
            int ry2 = (int)(20);

            FluidSimulation.DensityArray[FluidSimulation.Ix(rx2, ry2)] += 40 * new Vector4(0, 1, 0, 0) * Time.deltaTime;
            FluidSimulation.VelocityArray[FluidSimulation.Ix(rx2, ry2)] += new Vector2(Mathf.Cos(Time.time), Mathf.Sin(Time.time)) * 200 * Time.deltaTime;


        }


        for (int x=0; x<SimulationResolution; x++){
            for (int y = 0; y < SimulationResolution; y++) {

                 FluidSimulation.VelocityArray[FluidSimulation.Ix(x, y)] += new Vector2(Mathf.PerlinNoise(x/25f,y / 25f) *2-1f, Mathf.PerlinNoise(x /25f + 200,y / 25f) *2-1f)*Time.deltaTime*0.06f;



            }

        }

        transform.localEulerAngles+= new Vector3(0,1,0)*Time.deltaTime*30;
        FluidSimulation.SimStep(Time.deltaTime);
        UpdateTexture();
      // TakeSnapShot();
        

     
       // }
    }
}
