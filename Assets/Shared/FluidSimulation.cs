using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FluidSimulation{

    // This simulation is based on following paper
    // http://www.dgp.toronto.edu/people/stam/reality/Research/pdf/GDC03.pdf

    int Resolution;
    public float diffusionRate = 0.001f;

    // Simulation Data
    public List<Vector4> DensityArray;
    public List<Vector4> DensityArray0;

    public List<Vector2> VelocityArray;
    public List<Vector2> VelocityArray0;

    public List<Vector3> Mask;

    public FluidSimulation(){

        DensityArray= new List<Vector4>();
        DensityArray0 = new List<Vector4>();

        VelocityArray = new List<Vector2>();
        VelocityArray0 = new List<Vector2>();

        Mask = new List<Vector3>();

    }

    void SwapDensityArray(){

        //List<Vector4> temp= DensityArray;
        //DensityArray=DensityArray0;
        //DensityArray0= temp;
        for (int i = 0; i < DensityArray.Count; i++) DensityArray0[i] = DensityArray[i];


    }

    void SwapVelocityArray() {

        //List<Vector2> temp = VelocityArray;
        //VelocityArray = VelocityArray0;
        //VelocityArray0 = temp;
       for (int i = 0; i < VelocityArray.Count; i++) VelocityArray0[i] = VelocityArray[i];


    }

    public void setResolution(int resolution){

        Resolution= resolution;

        for(int i=0; i<resolution*resolution; i++){

            VelocityArray.Add(Vector2.zero);
            VelocityArray0.Add(Vector2.zero);

            DensityArray.Add(Vector4.zero);
            DensityArray0.Add(Vector4.zero);

            Mask.Add(Vector2.one);

        }

    }


    public void CreateMaskFromTexture(Texture2D maskTex){
       
        for(int x=0; x<Resolution; x++){
            for (int y = 0; y < Resolution; y++) {

                float nx = x / (float)(Resolution);
                float ny = y / (float)(Resolution);

                float sample= maskTex.GetPixel((int)Mathf.Floor(nx*maskTex.width), (int)Mathf.Floor(ny *maskTex.height)).r;

                Mask[Ix(x,y)]=new Vector3(sample,1,1);

            }

        }

        for (int x = 1; x < Resolution-1; x++) {
            for (int y = 1; y < Resolution-1; y++) {

                float l = Mask[Ix(x-1, y)].x;
                float r = Mask[Ix(x + 1, y)].x;
                float u = Mask[Ix(x , y+1)].x;
                float b = Mask[Ix(x , y-1)].x;

                Mask[Ix(x, y)] = new Vector3(Mask[Ix(x, y)].x,  r-l, u- b);

            }

        }

    }

    public void ClearDensities(){

        for(int i=0; i<DensityArray.Count; i++)
            DensityArray[i]= Vector4.zero;


    }

    public void ClearVelocity(){

        for (int i = 0; i < VelocityArray.Count; i++)
            VelocityArray[i] = Vector2.zero;

    }

    public int Ix(int x, int y) {
        return y * Resolution + x;
    }

    void UpdateBounds_Velocity(ref List<Vector2> velocity) {

        int N = Resolution - 2;

        for (int i = 1; i <= N; i++) {

            velocity[Ix(0, i)] =new Vector2(-velocity[Ix(1, i)].x, velocity[Ix(1, i)].y);
            velocity[Ix(N + 1, i)] = new Vector2(-velocity[Ix(N, i)].x, velocity[Ix(N, i)].y);

            velocity[Ix(i, 0)] = new Vector2(velocity[Ix(i, 1)].x, -velocity[Ix(i, 1)].y);
            velocity[Ix(i, N + 1)] = new Vector2(velocity[Ix(i, N)].x, -velocity[Ix(i, N)].y);

        }

        // Vertex
        velocity[Ix(0, 0)] = 0.5f * (velocity[Ix(1, 0)] + velocity[Ix(0, 1)]);
        velocity[Ix(0, N + 1)] = 0.5f * (velocity[Ix(1, N + 1)] + velocity[Ix(0, N)]);
        velocity[Ix(N + 1, 0)] = 0.5f * (velocity[Ix(N, 0)] + velocity[Ix(N + 1, 1)]);
        velocity[Ix(N + 1, N + 1)] = 0.5f * (velocity[Ix(N, N + 1)] + velocity[Ix(N + 1, N)]);



    }

    void UpdateBounds_Density() {

        int N = Resolution - 2;

        // Edges
        for (int i = 1; i <= N; i++) {

            DensityArray[Ix(0, i)] = DensityArray[Ix(1, i)];
            DensityArray[Ix(N + 1, i)] = DensityArray[Ix(N, i)];
            DensityArray[Ix(i, 0)] = DensityArray[Ix(i, 1)];
            DensityArray[Ix(i, N + 1)] = DensityArray[Ix(i, N)];

        }

        // Vertex
        DensityArray[Ix(0, 0)] = 0.5f * (DensityArray[Ix(1, 0)] + DensityArray[Ix(0, 1)]);
        DensityArray[Ix(0, N + 1)] = 0.5f * (DensityArray[Ix(1, N + 1)] + DensityArray[Ix(0, N)]);
        DensityArray[Ix(N + 1, 0)] = 0.5f * (DensityArray[Ix(N, 0)] + DensityArray[Ix(N + 1, 1)]);
        DensityArray[Ix(N + 1, N + 1)] = 0.5f * (DensityArray[Ix(N, N + 1)] + DensityArray[Ix(N + 1, N)]);

       
    }

    void AdvectDensity(float dt) {


        float bluut = 0;

        for (int x2 = 1; x2 < Resolution - 1; x2++) {
            for (int y2 = 1; y2 < Resolution - 1; y2++) {

                bluut += DensityArray0[Ix(x2, y2)].magnitude;
            }
        }



        int N = Resolution - 2;

        int i, j, i0, j0, i1, j1;
        float x, y, s0, t0, s1, t1, dt0;
        dt0 = dt * N;
        for (i = 1; i <= N; i++) {
            for (j = 1; j <= N; j++) {
                x = i - dt0 * VelocityArray[Ix(i, j)].x; y = j - dt0 * VelocityArray[Ix(i, j)].y;
                if (x < 0.5f) x = 0.5f; if (x > N + 0.5f) x = N + 0.5f; i0 = (int)x; i1 = i0 + 1;
                if (y < 0.5f) y = 0.5f; if (y > N + 0.5f) y = N + 0.5f; j0 = (int)y; j1 = j0 + 1;
                s1 = x - i0; s0 = 1 - s1; t1 = y - j0; t0 = 1 - t1;
                DensityArray[Ix(i, j)] = s0 * (t0 * DensityArray0[Ix(i0, j0)] + t1 * DensityArray0[Ix(i0, j1)])+
                    s1 * (t0 * DensityArray0[Ix(i1, j0)] + t1 * DensityArray0[Ix(i1, j1)]);
    }
}

UpdateBounds_Density();

        float bluut2 = 0;

        for (int x2 = 1; x2 < Resolution - 1; x2++) {
            for (int y2 = 1; y2 < Resolution - 1; y2++) {

                bluut2 += DensityArray[Ix(x2, y2)].magnitude;
            }
        }

        float lossFactor = 1;
        if (bluut2 != 0) lossFactor = bluut / bluut2;
        //Debug.Log("Loss_ "+ bluut+ "  " + bluut2);

        for (int x2 = 1; x2 < Resolution - 1; x2++) {
            for (int y2 = 1; y2 < Resolution - 1; y2++) {

                //DensityArray[Ix(x2,y2)]*=lossFactor;
            }
        }


        /*

int i0, j0, i1, j1;
        float x, y, s0, t0, s1, t1, dt0;

        dt0 = dt * N * 0.2f;


        for (int i = 1; i <= N; i++) {
            for (int j = 1; j <= N; j++) {

                x = i - dt0 * VelocityArray[Ix(i, j)].x;
                y = j - dt0 * VelocityArray[Ix(i, j)].y;

                if (x < 0.5f) x = 0.5f;
                if (x > N + 0.5f) x = N + 0.5f;
                i0 = (int)x;
                i1 = i0 + 1;

                if (y < 0.5) y = 0.5f;
                if (y > N + 0.5) y = N + 0.5f;
                j0 = (int)y;
                j1 = j0 + 1;

                s1 = x - i0;
                s0 = 1 - s1;
                t1 = y - j0;
                t0 = 1 - t1;


                DensityArray[Ix(i, j)] = s0 * (t0 * DensityArray0[Ix(i0, j0)] + t1 * DensityArray0[Ix(i0, j1)]) + s1 * (t0 * DensityArray0[Ix(i1, j0)] + t1 * DensityArray0[Ix(i1, j1)]);

            }
        }


        UpdateBounds_Density();
        */

    }

    void AdvectVelocity(float dt) {
       
        int N = Resolution - 2;
        int i, j, i0, j0, i1, j1;
        float x, y, s0, t0, s1, t1, dt0;
        dt0 = dt * N ;
        for (i = 1; i <= N; i++) {
            for (j = 1; j <= N; j++) {
                x = i - dt0 * VelocityArray0[Ix(i, j)].x;
                y = j - dt0 * VelocityArray0[Ix(i, j)].y;

                if (x < 0.5f) x = 0.5f;
                if (x > N + 0.5f) x = N + 0.5f; 

                i0 = (int)x;
                i1 = i0 + 1;

                if (y < 0.5f) y = 0.5f;
                if (y > N + 0.5f) y = N + 0.5f; 

                j0 = (int)y; 
                j1 = j0 + 1;
                s1 = x - i0;
                s0 = 1 - s1; 
                t1 = y - j0;
                t0 = 1 - t1;

                VelocityArray[Ix(i, j)] = s0 * (t0 * VelocityArray0[Ix(i0, j0)] + t1 * VelocityArray0[Ix(i0, j1)]) +
                    s1 * (t0 * VelocityArray0[Ix(i1, j0)] + t1 * VelocityArray0[Ix(i1, j1)]);
            }
        }


        UpdateBounds_Velocity(ref VelocityArray);
    }

    void ProjectVelocity() {

        // float * u, float * v, float * p, float * div 
        int N = Resolution - 2;
        int i, j, k;
        float h;
        h = 1.0f / N;

        for (i = 1; i <= N; i++) {
            for (j = 1; j <= N; j++) {
                VelocityArray0[Ix(i, j)] = new Vector2(0, -0.5f * h * (VelocityArray[Ix(i + 1, j)].x - VelocityArray[Ix(i - 1, j)].x + VelocityArray0[Ix(i, j + 1)].y - VelocityArray0[Ix(i, j - 1)].y));
            }
        }


        UpdateBounds_Velocity(ref VelocityArray0);

        for (k = 0; k < 10; k++) {
            for (i = 1; i <= N; i++) {
                for (j = 1; j <= N; j++) {

                    VelocityArray0[Ix(i, j)] = new Vector2((VelocityArray0[Ix(i, j)].y + VelocityArray0[Ix(i - 1, j)].x + VelocityArray0[Ix(i + 1, j)].x + VelocityArray0[Ix(i, j - 1)].x + VelocityArray0[Ix(i, j + 1)].x) / 4, VelocityArray0[Ix(i, j)].y);

                }
            }

            UpdateBounds_Velocity(ref VelocityArray0);
        }

        for (i = 1; i <= N; i++) {
            for (j = 1; j <= N; j++) {

                VelocityArray0[Ix(i, j)] -= new Vector2(0.5f * (VelocityArray0[Ix(i + 1, j)].x - VelocityArray0[Ix(i - 1, j)].x) / h, 0.5f * (VelocityArray0[Ix(i, j + 1)].x - VelocityArray0[Ix(i, j - 1)].x) / h);

            }
        }

        UpdateBounds_Velocity(ref VelocityArray);

    }



    void DiffuseDensity(float dt) {

        float a = dt * diffusionRate * Resolution * Resolution;

        for (int k = 0; k < 1; k++) {

            for (int x = 1; x < Resolution - 1; x++) {
                for (int y = 1; y < Resolution - 1; y++) {

                    DensityArray[Ix(x, y)] = (DensityArray0[Ix(x, y)] + a * (DensityArray[Ix(x - 1, y)] + DensityArray[Ix(x + 1, y)] + DensityArray[Ix(x, y - 1)] +  DensityArray[Ix(x, y + 1)])) / (1 + 4 * a);

                }
            }

            UpdateBounds_Density();
        }

    }


    void DiffuseVelocity(float dt) {


      float a = dt * diffusionRate * Resolution * Resolution;

        for (int k = 0; k < 10; k++) {

            for (int x = 1; x < Resolution - 1; x++) {
                for (int y = 1; y < Resolution - 1; y++) {

                    VelocityArray[Ix(x, y)] = (VelocityArray0[Ix(x, y)] + a * (VelocityArray[Ix(x - 1, y)] + VelocityArray[Ix(x + 1, y)] + VelocityArray[Ix(x, y - 1)] + VelocityArray[Ix(x, y + 1)])) / (1 + 4 * a);

                }
            }

            UpdateBounds_Velocity(ref VelocityArray);

        }

    }



    public void SimStep(float dt){

        UpdateBounds_Density();
        UpdateBounds_Velocity(ref VelocityArray);
        

        SwapVelocityArray();
        DiffuseVelocity(dt);
        ProjectVelocity();

        SwapVelocityArray();
        AdvectVelocity(dt);
        ProjectVelocity();

        SwapDensityArray();
        DiffuseDensity(dt);
        SwapDensityArray();
        AdvectDensity(dt);

    }


}
