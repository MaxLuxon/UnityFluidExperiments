using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IrisScene : MonoBehaviour {

    // This simulation is based on following paper
    // http://www.dgp.toronto.edu/people/stam/reality/Research/pdf/GDC03.pdf

    // Simulation Parameters
    public int Resolution;

    public bool DensityCorrection=false;

    // Add dye or not?
    public bool produce=true;

    // Whole simulation running?
    public bool sim = false;

    public bool EyeToMouse= false;

    // Simulation Data
    List<Vector4> DensityArray;
    List<Vector4> DensityArray0;

    List<Vector2> VelocityArray;
    List<Vector2> VelocityArray0;

    // Visual Objects
    Texture2D texture;

    public Color BackgroundColor;
    public Color Color1;
    public Color Color2;
    public Color Color3;

    public Material IrisMaterial;

    public Texture2D innerLines;
    public Texture2D ring;

    public GameObject pupil;
    public GameObject con;

    void Start () {

        texture = new Texture2D(Resolution,Resolution);
        texture.filterMode = FilterMode.Bilinear;
        GetComponent<MeshRenderer>().material.mainTexture = texture;

        IrisMaterial.SetTexture("_MainTex", texture);

        DensityArray = new List<Vector4>();
        DensityArray0 = new List<Vector4>();

        VelocityArray = new List<Vector2>();
        VelocityArray0 = new List<Vector2>();
        
        for (int i = 0; i < Resolution * Resolution; i++) {

            DensityArray0.Add(Vector4.zero);

            DensityArray.Add(Vector4.zero);
            VelocityArray.Add(new Vector2(Mathf.PerlinNoise(i%Resolution*100f+100,(int)(i/Resolution)*100f),Mathf.PerlinNoise(i%Resolution*0.001f+100,(int)(i/Resolution)*0.001f)));
            VelocityArray0.Add(new Vector2(Mathf.PerlinNoise(i%Resolution*100f+100,(int)(i/Resolution)*100f),Mathf.PerlinNoise(i%Resolution*0.001f+100,(int)(i/Resolution)*0.001f)));
            
        }
        
         for (int x = 0; x < Resolution; x++) {

            for (int y = 0; y < Resolution; y++) {
          
                float v_x=   -(Mathf.PerlinNoise(x/(float)Resolution*2.5f+300,y/(float)Resolution)*2.5f-1);
                float v_y=   Mathf.PerlinNoise(x/(float)Resolution*2.5f+1400,y/(float)Resolution)*2.5f-1;

                VelocityArray[Index(x,y)]= new Vector2(v_x,v_y).normalized*0.0f;
            
            }
            
          }

        UpdateBoundVelocity(VelocityArray);

	}

    int Index(int x, int y) {
        return y * Resolution + x;
    }

    // Mirror Velocities at the Bounds
    void UpdateBoundVelocity(List<Vector2> velocity) {

       int N = Resolution - 2;

        for (int i = 1; i <= N; i++) {

            velocity[Index(0, i)]     = new Vector2(-velocity[Index(1, i)].x, velocity[Index(1, i)].y);
            velocity[Index(N + 1, i)] = new Vector2(-velocity[Index(1, i)].x, velocity[Index(1, i)].y);
            velocity[Index(i, 0)]     = new Vector2( velocity[Index(1, i)].x, -velocity[Index(1, i)].y);
            velocity[Index(i, N + 1)] = new Vector2( velocity[Index(1, i)].x, -velocity[Index(1, i)].y);

        }

        // Vertex
        velocity[Index(0 ,0 )] = 0.5f*(velocity[Index(1,0 )]+velocity[Index(0 ,1)]);
        velocity[Index(0 ,N+1)] = 0.5f*(velocity[Index(1,N+1)]+velocity[Index(0 ,N )]);
        velocity[Index(N+1,0 )] = 0.5f*(velocity[Index(N,0 )]+velocity[Index(N+1,1)]);
        velocity[Index(N+1,N+1)] = 0.5f*(velocity[Index(N,N+1)]+velocity[Index(N+1,N )]);

    }

  
    // Mirror Density at the Bounds
    void UpdateDensityBounds (){

        int N = Resolution - 2;

        // Edges
        for (int i=1 ; i<=N ; i++ ) {

            DensityArray[Index(0 ,i)]  = DensityArray[Index(1,i)];
            DensityArray[Index(N+1,i)] = DensityArray[Index(N,i)];
            DensityArray[Index(i,0 )]  =  DensityArray[Index(i,1)];
            DensityArray[Index(i,N+1)] =  DensityArray[Index(i,N)];

        }

        // Vertex
        DensityArray[Index(0 ,0 )] = 0.5f*(DensityArray[Index(1,0 )]+DensityArray[Index(0 ,1)]);
        DensityArray[Index(0 ,N+1)] = 0.5f*(DensityArray[Index(1,N+1)]+DensityArray[Index(0 ,N )]);
        DensityArray[Index(N+1,0 )] = 0.5f*(DensityArray[Index(N,0 )]+DensityArray[Index(N+1,1)]);
        DensityArray[Index(N+1,N+1)] = 0.5f*(DensityArray[Index(N,N+1)]+DensityArray[Index(N+1,N )]);

    }

    // This funtion moves the density according to the velocities
    void AdvectDensity (){
   
        int N = Resolution - 2;

        int i0, j0, i1, j1;
        float x, y, s0, t0, s1, t1, dt0;
        UpdateDensityBounds ();

        dt0 = Time.deltaTime*N*0.2f;
     
        for (int i=1 ; i<=N ; i++ ) {
            for (int j=1 ; j<=N ; j++ ) {
               
                    x = i-dt0* VelocityArray[Index(i,j)].x; 
                    y = j-dt0* VelocityArray[Index(i,j)].y;

                    if (x<0.5f) x=0.5f; 
                    if (x>N+0.5f) x=N+ 0.5f; 
                    i0=(int)x;
                    i1=i0+1;
                    
                    if (y<0.5) y=0.5f; 
                    if (y>N+0.5) y=N+ 0.5f; 
                    j0=(int)y; 
                    j1=j0+1;

                    s1 = x-i0; 
                    s0 = 1-s1; 
                    t1 = y-j0; 
                    t0 = 1-t1;
                    
                     
                   DensityArray[Index(i,j)] = s0*(t0*DensityArray0[Index(i0,j0)]+t1*DensityArray0[Index(i0,j1)])+s1*(t0*DensityArray0[Index(i1,j0)]+t1*DensityArray0[Index(i1,j1)]);

            }
        }
       

        UpdateDensityBounds ();

     }

    // This funtion moves the Velocity according to the velocities
    void AdvectVelo (){
         
           int N= Resolution-2;
              int i, j, i0, j0, i1, j1;
        float x, y, s0, t0, s1, t1, dt0;
        dt0 = Time.deltaTime*N*0.2f;
        for ( i=1 ; i<=N ; i++ ) {
        for ( j=1 ; j<=N ; j++ ) {
        x = i-dt0*VelocityArray0[Index(i,j)].x; 
        y = j-dt0*VelocityArray0[Index(i,j)].y;
        if (x<0.5f) x=0.5f; if (x>N+0.5f) x=N+ 0.5f; i0=(int)x; i1=i0+1;
        if (y<0.5f) y=0.5f; if (y>N+0.5f) y=N+ 0.5f; j0=(int)y; j1=j0+1;
        s1 = x-i0; s0 = 1-s1; t1 = y-j0; t0 = 1-t1;
        VelocityArray[Index(i,j)] = s0*(t0*VelocityArray0[Index(i0,j0)]+t1*VelocityArray0[Index(i0,j1)])+
         s1*(t0*VelocityArray0[Index(i1,j0)]+t1*VelocityArray0[Index(i1,j1)]);
        }
        }
            
       UpdateBoundVelocity(VelocityArray);
     }


    // Not 100% sure what this function does
    void Project (){
           
        int N= Resolution-2;
        int i, j, k;
        float h = 1.0f/N;
        
        for ( i=1 ; i<=N ; i++ ) {
        for ( j=1 ; j<=N ; j++ ) {
            VelocityArray0[Index(i,j)] = new Vector2(0,-0.5f*h*(VelocityArray[Index(i+1,j)].x-VelocityArray[Index(i-1,j)].x+VelocityArray0[Index(i,j+1)].y-VelocityArray0[Index(i,j-1)].y));
        }
        }
        
        
       UpdateBoundVelocity(VelocityArray0);
        
        for ( k=0 ; k<10 ; k++ ) {
        for ( i=1 ; i<=N ; i++ ) {
        for ( j=1 ; j<=N ; j++ ) {
        
         VelocityArray0[Index(i,j)]= new Vector2((VelocityArray0[Index(i,j)].y+VelocityArray0[Index(i-1,j)].x+VelocityArray0[Index(i+1,j)].x+VelocityArray0[Index(i,j-1)].x+VelocityArray0[Index(i,j+1)].x)/4, VelocityArray0[Index(i,j)].y);

        }
        }
        
       UpdateBoundVelocity(VelocityArray0);
        }
        
        for ( i=1 ; i<=N ; i++ ) {
        for ( j=1 ; j<=N ; j++ ) {
        
            VelocityArray0[Index(i,j)]-= new Vector2( 0.5f*(VelocityArray0[Index(i+1,j)].x-VelocityArray0[Index(i-1,j)].x)/h, 0.5f*(VelocityArray0[Index(i,j+1)].x-VelocityArray0[Index(i,j-1)].x)/h);
        
        }
        }
        
       UpdateBoundVelocity(VelocityArray);
        
    }



    void DiffuseDensity() {
    
        float diff = 0.00002f;
        float a=Time.deltaTime*diff*Resolution*Resolution;

            for (int k=0 ; k<1 ; k++ ) {

                for (int x=1 ; x<Resolution-1 ; x++ ) {
                    for (int y=1 ; y<Resolution-1 ; y++ ) {

                        DensityArray[Index(x,y)] = (DensityArray0[Index(x,y)] + a*(DensityArray[Index(x-1,y)]+DensityArray[Index(x+1,y)]+DensityArray[Index(x,y-1)]+DensityArray[Index(x,y+1)]))/(1+4*a);

                    }
                }

                UpdateDensityBounds ();
            }

    }
    

    void DiffuseVelocity() {
    
        float diff = 0.0002f;
        float a=Time.deltaTime*diff*Resolution*Resolution;

            for (int k=0 ; k<10 ; k++ ) {

                for (int x=1 ; x<Resolution-1 ; x++ ) {
                    for (int y=1 ; y<Resolution-1 ; y++ ) {

                        VelocityArray[Index(x,y)] = (VelocityArray0[Index(x,y)] + a*(VelocityArray[Index(x-1,y)]+VelocityArray[Index(x+1,y)]+VelocityArray[Index(x,y-1)]+VelocityArray[Index(x,y+1)]))/(1+4*a);

                    }
                }

                UpdateBoundVelocity(VelocityArray);

            }

    }


    void Simulate() {
                 

        // In this block we add density (dye) & velocities to the map
        float productionFactor= (Mathf.Sin(Time.time+  Mathf.PI) * 0.2f + 0.7f)*2;
        
        int sx=(int)((Mathf.Sin(-Time.time*6.5f))*(Resolution*0.15f)+Resolution*0.5f);
        int sy=(int)((Mathf.Cos(Time.time*6.5f))*(Resolution*0.05f)+Resolution*0.5f);

        if(produce) DensityArray[sy*Resolution+sx] +=1*Time.deltaTime*new Vector4(1,0,0,0)*Resolution* productionFactor;
        VelocityArray[sy*Resolution+sx] += new Vector2(Mathf.Sin(-Time.time*6.5f),Mathf.Cos(Time.time*6.5f))*Time.deltaTime*1500;

        int sx2=(int)((Mathf.Sin(-Time.time*17.5f))*(Resolution*0.05f)+Resolution*0.5f);
        int sy2=(int)((Mathf.Cos(-Time.time*17.5f))*(Resolution*0.15f)+Resolution*0.5f);

        VelocityArray[sy2*Resolution+sx2] += new Vector2(Mathf.Sin(Time.time*17.5f),Mathf.Cos(Time.time*17.5f))*Time.deltaTime*500;
        if (produce) DensityArray[sy2*Resolution+sx2] += 1*Time.deltaTime*new Vector4(0,1,0,0)* Resolution* productionFactor;

        int sx3=(int)((Mathf.Sin(Time.time*6.5f))*(Resolution*0.15f)+Resolution*0.5f);
        int sy3=(int)((Mathf.Cos(Time.time*6.5f))*(Resolution*0.15f)+Resolution*0.5f);

        if (produce) DensityArray[sy3*Resolution+sx3]+=Time.deltaTime*1.4f*new Vector4(0,0,1,0)* Resolution* productionFactor; 
        VelocityArray[sy3*Resolution+sx3] += new Vector2(Mathf.Sin(Time.time*6.5f),Mathf.Cos(Time.time*6.5f))*Time.deltaTime*500;

        for(int i=0; i<DensityArray.Count; i++){
            DensityArray[i]-=new Vector4(1.0f,1,1,0)*Time.deltaTime*0.04f;
            DensityArray[i]= new Vector4(Mathf.Clamp(DensityArray[i].x,0,100),Mathf.Clamp(DensityArray[i].y,0,100),Mathf.Clamp(DensityArray[i].z,0,100),0);
        }


        for (int x4=1 ; x4<Resolution-1 ; x4++ ) {
                for (int y4=1 ; y4<Resolution-1 ; y4++ ) {

                    float nx= (float)x4/(float)Resolution;
                    float ny= (float)y4/(float)Resolution;
                    
                    Vector2 toCenter= new Vector2(Resolution/2.0f-x4, Resolution/2.0f-y4);
                                        
                    float sample= innerLines.GetPixel((int)(nx*innerLines.width),(int)(ny*innerLines.height)).r;
                    DensityArray[y4*Resolution+x4]-=Time.deltaTime*0.1f* DensityArray[y4 * Resolution + x4];
                    DensityArray[y4*Resolution+x4]-=Time.deltaTime*0.3f*new Vector4(1.2f,0,0,0)* DensityArray[y4*Resolution+x4].y * sample;
                    DensityArray[y4*Resolution+x4]-=Time.deltaTime*0.2f*new Vector4(0.0f,0,1,0)* DensityArray[y4*Resolution+x4].x * sample;

                    //VelocityArray[y4*Resolution+x4] += (new Vector2(Mathf.PerlinNoise(nx*6,ny*6)*2-1,Mathf.PerlinNoise(nx*6+45,ny*6)*2-1)*5)*Time.deltaTime*0.1f*sample*sample*sample;
 
                    float rsample= ring.GetPixel((int)(nx*ring.width),(int)(ny*ring.height)).r;
                    //                    float bsample= ring.GetPixel((int)(nx*ring.width),(int)(ny*ring.height)).b;

                    float fac= Mathf.Pow( Mathf.Max(toCenter.magnitude/Resolution*2-0.25f,0.0f)+ sample, 2);
                    VelocityArray[y4 * Resolution + x4] -= VelocityArray[y4 * Resolution + x4]*fac*Time.deltaTime*0.6f;

                    //VelocityArray[y4*Resolution+x4] -= (toCenter.normalized)*Time.deltaTime*0.1f*rsample;
                    //DensityArray[y4*Resolution+x4]+=Time.deltaTime*0.4f*new Vector4(0,1,1,0)*rsample;
                    VelocityArray[y4*Resolution+x4]+= (new Vector2(-toCenter.y, toCenter.x).normalized)*Time.deltaTime*6.1f*rsample;

           }
        }
           
        // Actual Simulation starts here
        UpdateDensityBounds ();
        UpdateBoundVelocity(VelocityArray);

        // This is inefficent, but swapping the "pointers" of the lists like in the original paper 
        // leads to an out of bounds exception :/
        for(int i=0; i<DensityArray.Count; i++) DensityArray0[i]=DensityArray[i];
        for(int i=0; i<VelocityArray.Count; i++) VelocityArray0[i]=VelocityArray[i];


        // The Advect Density function leads to a loss of Density
        // So in order to preserve the whole density we sum the density before and after the funtion call
        // And the adjust the and result
        float dSumBefore=0;
         
        for (int x2=1 ; x2<Resolution-1 ; x2++ ) {
            for (int y2=1 ; y2<Resolution-1 ; y2++ ) {

                dSumBefore+= DensityArray0[Index(x2,y2)].magnitude;
            
            }
        }
              
              
              

        DiffuseVelocity();

        Project ();
        for(int i=0; i<VelocityArray.Count; i++) VelocityArray0[i]=VelocityArray[i];

        AdvectVelo();
        Project ();

             
        DiffuseDensity();
        for(int i=0; i<DensityArray.Count; i++) DensityArray0[i]=DensityArray[i];

        AdvectDensity();

        float dSumAfter =0;

        for (int x2 = 1; x2 < Resolution - 1; x2++) {
            for (int y2 = 1; y2 < Resolution - 1; y2++) {

                dSumAfter += DensityArray0[Index(x2, y2)].magnitude;

            }
        }


        if(DensityCorrection){

            float lossFactor = 1;
            if(dSumAfter != 0) lossFactor=dSumBefore/ dSumAfter;
          
             for (int x2=1 ; x2<Resolution-1 ; x2++ ) {
                for (int y2=1 ; y2<Resolution-1 ; y2++ ) {

                   DensityArray[Index(x2,y2)]*=lossFactor;
                }
            }
               
        }

   
    }

    // Turn the density data into an image
    void UpdateImage() {

       List<Color> color = new List<Color>();
       for (int i = 0; i < DensityArray.Count; i++) {
       
            Color c=Color.Lerp(BackgroundColor, Color1, DensityArray[i].x);
            c+=Color.Lerp(Color.black, Color2, DensityArray[i].y);
            c+=Color.Lerp(Color.black, Color3, DensityArray[i].z);
            color.Add(c);

        }

        texture.SetPixels(0, 0, Resolution, Resolution, color.ToArray());
        texture.Apply();

    }

	// Update is called once per frame
	void Update () {

        float mx=(Input.mousePosition.x/Screen.width-0.5f)*2;
        float my = (Input.mousePosition.y / Screen.height - 0.5f) * 2;

        if(EyeToMouse)
        con.transform.eulerAngles= new Vector3(my * 30, -mx*30,0);

        pupil.transform.localScale = Vector3.one * (0.08f + (Mathf.Sin(Time.time + Mathf.PI) * 0.5f + 0.5f) * 0.07f);

        if (Input.GetKeyDown(KeyCode.P))
        sim=!sim;

        if (Input.GetKeyDown(KeyCode.L))
            produce = !produce;

        if (sim)
        Simulate();


        UpdateImage();

	}
}
