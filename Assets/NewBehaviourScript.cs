using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;



public class NewBehaviourScript : MonoBehaviour {
    // Start is called before the first frame update
    public GameObject plGameObject;
	public GameObject cameraa;
    public Vector3 plVel;
	public Vector3 plPos;
    public static GameObject track;
    public TextMeshProUGUI text;

	public float plSpeed = 0.0015f;
	float mouseAngle = 0;
	public bool plDead = false;
    public bool phaseShifting = false;
    public bool godMode = false;
    public int score = 0;
    public float collectCircleSize;
    public float frictionCoefficient = 0.0001f;
	
    Sprite plSprite1;
    Sprite plSprite2; //transparent
    Sprite plSprite3; //Invincible
    
    public Vector3 mousePos;
    
    public static int maxTrackIncreaseCounter = 3;
    public static int trackIncreaseCounter = 0;
    
    
    void Start() {
		Debug.Log("Hello World");
        
        plGameObject = GameObject.Find("Player");
		cameraa = GameObject.Find("Main Camera");
		
        plSprite1 = plGameObject.GetComponent<SpriteRenderer>().sprite;
        plSprite2 = Resources.Load<Sprite>("Player4");
        plSprite3 = Resources.Load<Sprite>("Player5");
        Coin.sprite =  Resources.Load<Sprite>("Coin");
        Mine.sprite =  Resources.Load<Sprite>("Mine");
        Explosion.sprite =  Resources.Load<Sprite>("Explosion");
        
        
        
		plPos = new Vector3(38,0,0);
		plVel = new Vector3(0,0,0);
        
        track = GameObject.Find("Track");
        Mesh mesh = track.GetComponent<MeshFilter>().mesh;
        Bezier.mesh = mesh;
        Bezier.line = track.GetComponent<LineRenderer>();
        Bezier.line.positionCount = Bezier.numApproxPoints;
        track.transform.localPosition = new Vector3(0,0,1);
        
        Vector3[] verts = new Vector3[Bezier.approxSidePoints];
        
        new Bezier(10,-5,10,0);
        new Bezier(20,5,20,0);
        new Bezier(30,-10,30,0);
        new Bezier(35,10,40,0);
        new Bezier(40,0,50,10);
        
        
        //setup mesh
        float stride = (float)Bezier.numBeziers / (float)Bezier.approxSidePoints;
        float total = 0;
        float side = -1.0f;
        int ind = 0;
        
        for (int i = 0; i < Bezier.approxSidePoints; i++) {
            Vector3 a = Bezier.bezierArray[ind].derivativeAtTime(total).normalized * Bezier.bezierWidth;
            Vector3 b = Bezier.bezierArray[ind].pointAtTime(total);
            verts[i] = new Vector3(b.x + side*a.y,b.y + -side*a.x, 0);
            
            
            total += stride;
            if (total >= 1.0f) {
                total -= 1.0f;
                ind+=1;
            }
            side *= -1;
        }
        
        mesh.vertices = verts;
        
        int[] triangles = new int[3*(Bezier.approxSidePoints-2)];
        for (int i = 0; i < Bezier.approxSidePoints-2; i++) {
            triangles[3*i]=i;
            triangles[3*i+1]=i+1;
            triangles[3*i+2]=i+2;
        }
        
        mesh.triangles = triangles;
        
        //setup approx points
        ind = 0;
        for (int i = 0; i < Bezier.numApproxPoints; i++) {
            Vector3 b = Bezier.bezierArray[ind].pointAtTime(Bezier.approxHead);
            Vector3 c = Bezier.bezierArray[ind].derivativeAtTime(Bezier.approxHead);
            Bezier.approx[i] = new Vector3(b.x, b.y, 0);
            Bezier.approx2[i] = new Vector3(c.x,c.y,0);
            Bezier.linePoints[i] = new Vector3(b.x, b.y, 0);
            
            
            Bezier.approxHead += Bezier.approxStride;
            if (Bezier.approxHead >= 1.0f) {
                Bezier.approxHead -= 1.0f;
                ind+=1;
            }
        }
        
        //stop frustrum culling
        mesh.bounds = new Bounds(transform.position, new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));

        
        //setup some other stuff
        collectCircleSize = 0.3f + Objs.coinSize;
        collectCircleSize *= collectCircleSize;
    }
    
    // Update is called once per frame
    void Update() {
		//take inputs
		if (!plDead) {
            if (!phaseShifting) {
                mousePos = Input.mousePosition; 
		        mouseAngle = Mathf.Atan2(mousePos.y-Screen.height/2,mousePos.x-Screen.width/2);
                if (Mathf.Cos(mouseAngle) == 0.0f) {
                    mouseAngle+=0.000000001f;
                }
        
		        if (Input.GetMouseButton(0)) {
		        	plVel.x += plSpeed * Mathf.Cos(mouseAngle);
		        	plVel.y += plSpeed * Mathf.Sin(mouseAngle);
                }
            }
            
            if (Input.GetKeyDown(KeyCode.LeftShift)) {
                phaseShifting = true;
                plGameObject.GetComponent<SpriteRenderer>().sprite = plSprite2;
            } else if (Input.GetKeyUp(KeyCode.LeftShift)) {
                phaseShifting = false;
                plGameObject.GetComponent<SpriteRenderer>().sprite = plSprite1;
            }
            
            //invincibility cheat
            if (Input.GetKeyDown(KeyCode.P)) {
                 if (Input.GetKeyDown(KeyCode.O)) {
                     if (Input.GetKeyDown(KeyCode.G)) {
                        godMode = true;
                        plGameObject.GetComponent<SpriteRenderer>().sprite = plSprite3;
                     }
                 }
            }
            
            
        }
		
        bool death = false;
        
        //Test if player is on track
        bool onTrack = false;
        int farthestUp = Bezier.approxZero;
        int n = Bezier.approxZero;
        for (int i = 0; i < Bezier.numApproxPoints; i++) {
            Vector3 der = Bezier.approx2[i];
            Vector3 p = Bezier.approx[i];
            if (Vector3.Dot(der, plPos - p) > 0.0f) {
                if ((p-plPos).sqrMagnitude < Bezier.bezierWidthSquared) {
                    onTrack = true;
                    farthestUp = (i - Bezier.approxZero + Bezier.numApproxPoints) % Bezier.numApproxPoints;
                }
            }
        }
        
        if (!onTrack && !phaseShifting) {
            death = true;
        }
        
        //move track up
        if (farthestUp > Bezier.newTrackThreshold) {
            Bezier.extendTrack(farthestUp - Bezier.newTrackThreshold);
        }
        
        trackIncreaseCounter+=1;
        if (trackIncreaseCounter == maxTrackIncreaseCounter) {
            trackIncreaseCounter = 0;
            Bezier.extendTrack(1);
        }
        
        //coins
        for (int i=0;i<Objs.coins.Count; i++) {
            Vector3 a = plPos - Objs.coins[i].pos;
            if (a.x*a.x+a.y*a.y < collectCircleSize) {
                score += 1;
                Destroy(Objs.coins[i].obj);
                Objs.coins.RemoveAt(i);
                i--;
            }
        }
        
        //mines
        for (int i=0;i<Objs.mines.Count; i++) {
            Vector3 a = plPos - Objs.mines[i].pos;
            if (!phaseShifting && a.x*a.x+a.y*a.y < Objs.mineSensorSize) {
                
                Objs.explosions.Add(new Explosion(Objs.mines[i].pos, Objs.mineSize, Objs.mineExplosionSize, Objs.mineExplosionSpeed));
                
                
                Destroy(Objs.mines[i].obj);
                Objs.mines.RemoveAt(i);
                i--;
            }
        }
        
        //explosion logic
        for (int i=0;i<Objs.explosions.Count; i++) {
            Vector3 a = plPos - Objs.explosions[i].pos;
            
            
            
            if (a.x*a.x+a.y*a.y < Objs.explosions[i].radius*Objs.explosions[i].radius / 2.0f) {
                death = true;
            }
            
            if (Objs.explosions[i].radius > Objs.explosions[i].maxRadius) {
                Destroy(Objs.explosions[i].obj);
                Objs.explosions.RemoveAt(i);
                i--;
            } else {
                Objs.explosions[i].radius += Objs.explosions[i].speed;
                Objs.explosions[i].obj.transform.localScale = new Vector3(Objs.explosions[i].radius, Objs.explosions[i].radius, 1.0f);
            }
        }
        
        
        
        //kill player
        if (death && !phaseShifting && !godMode) {
            plDead = true;
        }
        
        //friction
        if (onTrack && !phaseShifting && !death) {
            Vector3 friction;
            //friction.x = (plVel.x * Mathf.Cos(mouseAngle) + plVel.y * Mathf.Sin(mouseAngle))*Mathf.Cos(mouseAngle);
            //friction.y = friction.x * Mathf.Tan(mouseAngle) - plVel.y;
            //friction.x -= plVel.x;
            
            //friction = -1.0f*plVel.normalized * Mathf.Abs(Mathf.Sin(Mathf.Atan2(plVel.y, plVel.x) - mouseAngle));
            
            
            //float d = (plVel.x * Mathf.Sin(mouseAngle) - plVel.y * Mathf.Cos(mouseAngle)) / plVel.magnitude;
            //
            //friction = 
            //    -d * new Vector3(Mathf.Sin(mouseAngle), -Mathf.Cos(mouseAngle), 0) + 
            //    Mathf.Abs(d) * new Vector3(Mathf.Cos(mouseAngle), Mathf.Sin(mouseAngle), 0);
            
            float invmag = 1.0f/plVel.magnitude;
            float dot = (plVel.x * Mathf.Cos(mouseAngle) + plVel.y * Mathf.Sin(mouseAngle)) * invmag;
            friction = (dot-1.0f) * invmag * plVel;
            
            //friction += new Vector3(Mathf.Cos(mouseAngle), Mathf.Sin(mouseAngle), 0);
            
            if (friction.sqrMagnitude > frictionCoefficient*frictionCoefficient) {
                plVel += friction * frictionCoefficient;
            }
            
            
            
        }
    
		plPos += plVel;
		
        
		//render
        plGameObject.transform.localPosition = plPos;
		cameraa.transform.localPosition = plPos + new Vector3(0,0,-10);
		plGameObject.transform.eulerAngles = new Vector3(0,0,mouseAngle*180.0f/Mathf.PI);
        
        //Mesh mesh = track.GetComponent<MeshFilter>().mesh;
        //mesh.bounds.center
        
        text.text = score.ToString();
    }
}

public class Bezier {
    
	public static int numBeziers = 5;
    
    public static int accuracy = 100;
    public static int numApproxPoints = accuracy * numBeziers; // point used for approximations of the bezier line
	public static int approxSidePoints = accuracy * 2 * numBeziers; //points on sides of the bezier for the mesh. More is more smooth.
	public static float bezierWidth = 2.5f;
    public static float bezierWidthSquared = bezierWidth*bezierWidth;
    
    //circluar array for the spline
    public static Bezier[] bezierArray = new Bezier[numBeziers];
    public static int bezierArrayZero = 0;
    
    
    //approx points
    public static Vector3[] approx = new Vector3[numApproxPoints];
    public static Vector3[] approx2 = new Vector3[numApproxPoints];
    public static float approxStride = (float)numBeziers / (float)numApproxPoints;
    public static float approxHead = 0.0f; //approx point first in the beziers
    public static int approxZero = 0;
    public static int gfxApproxZero = 0;
    
    
    //last 2 points
    public static float lastx3 = 0;
    public static float lasty3 = 0;
    public static float lastx4 = 0;
    public static float lasty4 = 0;
    
    //for track gen
    static float newBezierp1RandDist = 10.0f;
    static float newBezierp1MinDist = 4.0f;
    static float newBezierp2RandDist = 10.0f;
    static float newBezierp2MinDist = 10.0f;
    public static int newTrackThreshold = numApproxPoints - 200;
    public static float newBezierChordSize = Mathf.PI;
    
    public static Mesh mesh;
    public static LineRenderer line;
    
    
    
    public static Vector3[] linePoints = new Vector3[numApproxPoints];

		
	float ax; //proportional to t^3
    float bx;
    float cx;
    float dx;
    float ay;
    float by;
    float cy;
    float dy;
    
    float dax; //proportional to t^2
    float dbx;
    float dcx;
    float day;
    float dby;
    float dcy;
    
    public Bezier(float x3,float y3,float x4,float y4) {
        float x1 = lastx4;
        float y1 = lasty4;
        float x2 = 2*lastx4 - lastx3;
        float y2 = 2*lasty4 - lasty3;
        lastx3 = x3;
        lasty3 = y3;
        lastx4 = x4;
        lasty4 = y4;
        
        
        
        //points to polynomials
        ax = (x1*-1)+(3*x2)+(-3*x3)+(x4);
        bx = (x1*3)+(x2*-6)+(x3*3);
        cx = (x1*-3)+(x2*3);
        dx = (x1*1);
        ay = (y1*-1)+(3*y2)+(-3*y3)+(y4);
        by = (y1*3)+(y2*-6)+(y3*3);
        cy = (y1*-3)+(y2*3);
        dy = (y1*1);
        
        //derivative
        dax = (x1*-3)+(x2*9)+(x3*-9)+(x4*3);
        dbx = (x1*6)+(x2*-12)+(x3*6);
        dcx = (x1*-3)+(x2*3);
        day = (y1*-3)+(y2*9)+(y3*-9)+(y4*3);
        dby = (y1*6)+(y2*-12)+(y3*6);
        dcy = (y1*-3)+(y2*3);
        
        //append to array
        bezierArray[bezierArrayZero] = this;
        bezierArrayZero=(bezierArrayZero+1)%numBeziers;
    }
    
    public Vector3 pointAtTime(float t) {
        return new Vector3(
            ax*t*t*t + bx*t*t + cx*t + dx,
            ay*t*t*t + by*t*t + cy*t + dy,
            0
        );
    }
    
    public Vector3 derivativeAtTime(float t) {
        return new Vector3(
            dax*t*t + dbx*t + dcx,
            day*t*t + dby*t + dcy,
            0
        );
    }
        
    //extend to track by specified amount
    public static void extendTrack(int amount) {
        Vector3[] verts = mesh.vertices;
        int[] triangles = mesh.triangles; //new int[3*(Bezier.approxSidePoints-2)];
        
        
        while (amount > 0) {
            Bezier bez;
            if (approxZero % accuracy == 0) {
                approxZero %= accuracy * numBeziers;
                
                float a1 = Mathf.Atan2(lasty4-lasty3, lastx4-lastx3) + Random.value*newBezierChordSize - newBezierChordSize / 2.0f;
                float a2 = Mathf.Atan2(lasty4-lasty3, lastx4-lastx3) + Random.value*newBezierChordSize - newBezierChordSize / 2.0f;
                
                float d1 = Random.value * newBezierp1RandDist + newBezierp1MinDist;           
                float d2 = Random.value * newBezierp2RandDist + newBezierp2MinDist;     
                
                bez = new Bezier(
                    lastx4 + Mathf.Cos(a1)*d1,
                    lasty4 + Mathf.Sin(a1)*d1,
                    lastx4 + Mathf.Cos(a2)*d2,
                    lasty4 + Mathf.Sin(a2)*d2
                );
            } else {
                bez = bezierArray[(bezierArrayZero+numBeziers-1)%numBeziers];
            }
            
            Bezier.approxHead += Bezier.approxStride;
            if (Bezier.approxHead >= 1.0f) {
                Bezier.approxHead -= 1.0f;
            }
            
            Vector3 a = bez.pointAtTime(Bezier.approxHead);
            Vector3 b = bez.derivativeAtTime(Bezier.approxHead);
            Bezier.approx[approxZero] = new Vector3(a.x, a.y, 0);
            Bezier.approx2[approxZero] = new Vector3(b.x,b.y,0);
            b = b.normalized * bezierWidth;
            
            
            verts[approxZero*2] = new Vector3(a.x - b.y,a.y + b.x, 0);
            verts[approxZero*2+1] = new Vector3(a.x + b.y,a.y - b.x, 0);
            Vector3 p1 = new Vector3(0,0,0) + verts[approxZero*2];
            Vector3 p2 = new Vector3(0,0,0) + verts[approxZero*2+1];
            
            
            //Debug.Log(approxZero);
            triangles[(gfxApproxZero*6)% (3*approxSidePoints-6)] =   (2*approxZero+-2 + (approxSidePoints-2))  % (approxSidePoints-2);
            triangles[(gfxApproxZero*6+1)% (3*approxSidePoints-6)] = (2*approxZero+-1 + (approxSidePoints-2))  % (approxSidePoints-2);
            triangles[(gfxApproxZero*6+2)% (3*approxSidePoints-6)] = (2*approxZero+0  + (approxSidePoints-2))  % (approxSidePoints-2);
            triangles[(gfxApproxZero*6+3)% (3*approxSidePoints-6)] = (2*approxZero+-1 + (approxSidePoints-2))  % (approxSidePoints-2);
            triangles[(gfxApproxZero*6+4)% (3*approxSidePoints-6)] = (2*approxZero+0  + (approxSidePoints-2))  % (approxSidePoints-2);
            triangles[(gfxApproxZero*6+5)% (3*approxSidePoints-6)] = (2*approxZero+1  + (approxSidePoints-2))  % (approxSidePoints-2);
            
            
            //there has to be a better way to do this.
            for (int i = 0; i < numApproxPoints-1; i++) {
                linePoints[i] = linePoints[i+1];
            }
            
            linePoints[numApproxPoints-1] = new Vector3(a.x, a.y, 0);

            approxZero = (approxZero + 1) % numApproxPoints;
            gfxApproxZero = (gfxApproxZero + 1) % (Bezier.numApproxPoints-1);
        
            //approxHead += approxStride;
            
            
            amount-=1;   
            
            //random entities
            
            
            //coin spawn
            if (Random.value < Objs.coinChance) {
                Vector3 npos = p1 + Random.value * (p2 - p1);
                Objs.coins.Add(new Coin(npos));
            }
            
            //mine spawn
            if (Random.value < Objs.coinChance) {
                Vector3 npos = p1 + Random.value * (p2 - p1);
                Objs.mines.Add(new Mine(npos));
            }
            
            
            
        }
        
        mesh.vertices = verts;
        mesh.triangles = triangles;
        
        line.SetPositions(linePoints);
        
        
    }
}

public class Objs {
    public static List<Coin> coins = new List<Coin>();
    public static List<Mine> mines = new List<Mine>();
    public static List<Explosion> explosions = new List<Explosion>();
    public static float coinSize = 0.65f;
    public static float mineSize = 0.65f;
    public static float mineSensorSize = 6.0f; //this is the SQUARE of the distance from the mine center
    public static float mineExplosionSpeed = 0.02f;
    public static float mineExplosionSize = 3.0f;
    public static float coinChance = 0.01f;
    public static float mineChance = 0.01f;
    
    
    public static int numSpawns = 0;
    
}

public class Coin {
    public Vector3 pos;
    public GameObject obj;
    
    public static Sprite sprite;
    
    public Coin(Vector3 a) {
        pos=a;
        Objs.numSpawns+=1;
        obj = new GameObject("Coin" + Objs.numSpawns.ToString());
        obj.AddComponent<SpriteRenderer>();
        obj.GetComponent<SpriteRenderer>().sprite = sprite;
        obj.transform.localPosition = pos;
        obj.transform.localScale = new Vector3(Objs.coinSize, Objs.coinSize, 1.0f);
    }
}

public class Mine {
    public Vector3 pos;
    public GameObject obj;
    public static Sprite sprite;
    
    public Mine(Vector3 a) {
        pos=a;
        Objs.numSpawns+=1;
        obj = new GameObject("Mine" + Objs.numSpawns.ToString());
        obj.AddComponent<SpriteRenderer>();
        obj.GetComponent<SpriteRenderer>().sprite = sprite;
        obj.transform.localPosition = pos;
        obj.transform.localScale = new Vector3(Objs.mineSize, Objs.mineSize, 1.0f);
    }
}

public class Explosion {
    public Vector3 pos;
    public float radius;
    public float maxRadius;
    public float speed;
    public GameObject obj;
    public static Sprite sprite;
    
    public Explosion(Vector3 a, float b, float c, float d) {
        pos=a;
        radius=b;
        maxRadius=c;
        speed=d;
        
        Objs.numSpawns+=1;
        obj = new GameObject("Explosion" + Objs.numSpawns.ToString());
        obj.AddComponent<SpriteRenderer>();
        obj.GetComponent<SpriteRenderer>().sprite = sprite;
        obj.transform.localPosition = pos;
        obj.transform.localScale = new Vector3(b, b, 1.0f);
    }
}