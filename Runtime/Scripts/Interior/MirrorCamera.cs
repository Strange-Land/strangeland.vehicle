using UnityEngine;
using UnityEngine.Rendering;

public class MirrorCamera : MonoBehaviour
{
    private const int FrameSkip = 3;
    private static int SharedFrameOffset;

    [Header("Render Target")]
    public Material ReferenceMaterial;
    public int width = 256;
    public int height = 256;
    public RenderTextureFormat rtf = RenderTextureFormat.ARGB32;

    [Header("Mirror Setup")]
    public Transform mirrorPlane;     // <-- set this (child). mirrorPlane.up = normal
    public Camera viewerCamera;       // if null: Camera.main
   
    public float clipNear = 0.07f;
    public float clipFar = 0.07f;
    public float fov = 0.07f;
    private MeshRenderer targetRenderer;
    private RenderTexture rt;
    private Camera mirrorCam;
    private int frameOffset;
    
    
    


    void Start()
    {
        mirrorCam = GetComponentInChildren<Camera>(true);
    
        
        targetRenderer = GetComponent<MeshRenderer>();

        if (mirrorPlane == null) mirrorPlane = transform; // fallback

        rt = new RenderTexture(width, height, 16, rtf);
        rt.name = "MirrorCameraTexture_" + name;
        rt.Create();

        var mat = new Material(ReferenceMaterial);
        mat.name = "material Copy" + transform.name;
        mat.SetTexture("_BaseMap", rt);
        targetRenderer.material = mat;

        
        mirrorCam.forceIntoRenderTexture = true;
        mirrorCam.targetTexture = rt;
        mirrorCam.enabled = false;
        mirrorCam.fieldOfView = fov;        // tweak per mirror (side vs rear)
        mirrorCam.nearClipPlane = clipNear;
        mirrorCam.farClipPlane = clipFar;

        frameOffset = SharedFrameOffset++;
    }

    void LateUpdate()
    {
        if (viewerCamera == null) viewerCamera = Camera.main;
        if (viewerCamera == null || mirrorCam == null) return;

        UpdateMirrorCameraPose(viewerCamera, mirrorCam);

        if ((Time.frameCount + frameOffset) % FrameSkip == 0)
            mirrorCam.Render();
    }

    private void UpdateMirrorCameraPose(Camera viewer, Camera mirror)
    {

        Vector3 mirrorNormal = mirrorPlane.forward;
        Vector3 mirrorPosition = mirrorPlane.position;
        Vector3 cameraPosition = viewerCamera.transform.position;
        Vector3 UpReference = transform.parent.parent.up;
        
        Debug.DrawRay(mirrorPosition,mirrorNormal*5,Color.red);
        
        
        Vector3 relativePosition = mirrorPosition-cameraPosition ;
        Debug.DrawRay(cameraPosition,relativePosition,Color.green);
        
        
        Vector3 reflected = Vector3.Reflect(relativePosition,mirrorNormal);
        Debug.DrawRay(mirrorPosition,5*reflected,Color.blue);
        
        Vector3 mirroredPos = mirrorPosition + reflected;
        
        mirrorCam.transform.position = mirroredPos;
        mirrorCam.transform.rotation =
            Quaternion.LookRotation(reflected, UpReference);
        
        mirrorCam.fieldOfView = fov;       
        mirrorCam.nearClipPlane = clipNear;
        mirrorCam.farClipPlane = clipFar;
    }

    private static Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign, float offset)
    {
        Vector3 offsetPos = pos + normal * offset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cPos = m.MultiplyPoint(offsetPos);
        Vector3 cNormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cNormal.x, cNormal.y, cNormal.z, -Vector3.Dot(cPos, cNormal));
    }

    void OnDestroy()
    {
        if (rt != null) rt.Release();
    }
}