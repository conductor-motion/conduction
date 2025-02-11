using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mediapipe.BlazePose;
using System.IO;
using Google.Protobuf;
using System.Collections;
using UnityEngine.SceneManagement;

public class PoseVisuallizer3D : MonoBehaviour
{
    [SerializeField] Camera mainCamera;
    [SerializeField] WebCamInput webCamInput;
    [SerializeField] RawImage inputImageUI;
    [SerializeField] Shader shader;
    [SerializeField, Range(0, 1)] float humanExistThreshold = 0.5f;

    Material material;
    BlazePoseDetecter detecter;

    private FileStream dataFile;
    private StreamWriter writer;
    string filePath;
    string dirPath;
    int frameIndex = 0;
    Frames frames = new Frames();
    List<HandMovementData> data = new List<HandMovementData>();

    Transform objToPickUp;
    public Animator animator;

    public Transform rightHandTarget;
    public Transform leftHandTarget;
    public Transform rightElbowTarget;
    public Transform leftElbowTarget;
    public Transform rightFootTarget;
    public Transform leftFootTarget;
    public Transform rightKneeTarget;
    public Transform leftKneeTarget;



    // Lines count of body's topology.
    const int BODY_LINE_NUM = 35;
    // Pairs of vertex indices of the lines that make up body's topology.
    // Defined by the figure in https://google.github.io/mediapipe/solutions/pose.
    readonly List<Vector4> linePair = new List<Vector4>{
        new Vector4(0, 1), new Vector4(1, 2), new Vector4(2, 3), new Vector4(3, 7), new Vector4(0, 4),
        new Vector4(4, 5), new Vector4(5, 6), new Vector4(6, 8), new Vector4(9, 10), new Vector4(11, 12),
        new Vector4(11, 13), new Vector4(13, 15), new Vector4(15, 17), new Vector4(17, 19), new Vector4(19, 15),
        new Vector4(15, 21), new Vector4(12, 14), new Vector4(14, 16), new Vector4(16, 18), new Vector4(18, 20),
        new Vector4(20, 16), new Vector4(16, 22), new Vector4(11, 23), new Vector4(12, 24), new Vector4(23, 24),
        new Vector4(23, 25), new Vector4(25, 27), new Vector4(27, 29), new Vector4(29, 31), new Vector4(31, 27),
        new Vector4(24, 26), new Vector4(26, 28), new Vector4(28, 30), new Vector4(30, 32), new Vector4(32, 28)
    };


    void Start()
    {
        material = new Material(shader);
        detecter = new BlazePoseDetecter();

        if(SceneManager.GetActiveScene().name == "RecordingPage")
            CreateDataFile();
    }

    void LateUpdate()
    { 
        inputImageUI.texture = webCamInput.inputImageTexture;

        // Predict pose by neural network model.
        detecter.ProcessImage(webCamInput.inputImageTexture);

        FrameData frame = new FrameData(frameIndex);
        // Output landmark values(33 values) and the score whether human pose is visible (1 values).
        for (int i = 0; i < detecter.vertexCount + 1; i++)
        {
            /*
            0~32 index datas are pose world landmark.
            Check below Mediapipe document about relation between index and landmark position.
            https://google.github.io/mediapipe/solutions/pose#pose-landmark-model-blazepose-ghum-3d
            Each data factors are
            x, y and z: Real-world 3D coordinates in meters with the origin at the center between hips.
            w: The score of whether the world landmark position is visible ([0, 1]).
        
            33 index data is the score whether human pose is visible ([0, 1]).
            This data is (score, 0, 0, 0).
            */
            //Debug.LogFormat("{0}: {1}", i, detecter.GetPoseWorldLandmark(i));

            if (RecordingController.isRecording && SceneManager.GetActiveScene().name == "RecordingPage" && (i == 15 || i == 16))
            {
                frame.data.Add(new HandMovementData(i, detecter.GetPoseWorldLandmark(i).x, detecter.GetPoseWorldLandmark(i).y, detecter.GetPoseWorldLandmark(i).z, detecter.GetPoseWorldLandmark(i).w));

            }
        }
        if (RecordingController.isRecording && SceneManager.GetActiveScene().name == "RecordingPage")
        {
            frames.frames.Add(frame);
            frameIndex++;
        }

        Vector3 temp = new Vector3();


        temp.x = detecter.GetPoseWorldLandmark(15).x;
        temp.y = detecter.GetPoseWorldLandmark(15).y;
        temp.z = detecter.GetPoseWorldLandmark(15).z;
        leftHandTarget.position = temp;


        temp.x = detecter.GetPoseWorldLandmark(16).x;
        temp.y = detecter.GetPoseWorldLandmark(16).y;
        temp.z = detecter.GetPoseWorldLandmark(16).z;
        rightHandTarget.position = temp;


        temp.x = detecter.GetPoseWorldLandmark(14).x;
        temp.y = detecter.GetPoseWorldLandmark(14).y;
        temp.z = detecter.GetPoseWorldLandmark(14).z;
        rightElbowTarget.position = temp;


        temp.x = detecter.GetPoseWorldLandmark(13).x;
        temp.y = detecter.GetPoseWorldLandmark(13).y;
        temp.z = detecter.GetPoseWorldLandmark(13).z;
        leftElbowTarget.position = temp;

        //Debug.Log("---");
    }

    /*void OnRenderObject(){
        // Use predicted pose world landmark results on the ComputeBuffer (GPU) memory.
        material.SetBuffer("_worldVertices", detecter.worldLandmarkBuffer);
        // Set pose landmark counts.
        material.SetInt("_keypointCount", detecter.vertexCount);
        material.SetFloat("_humanExistThreshold", humanExistThreshold);
        material.SetVectorArray("_linePair", linePair);
        material.SetMatrix("_invViewMatrix", mainCamera.worldToCameraMatrix.inverse);

        // Draw 35 world body topology lines.
        material.SetPass(2);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, BODY_LINE_NUM);

        // Draw 33 world landmark points.
        material.SetPass(3);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, 6, detecter.vertexCount);
    }*/

    void OnDestroy()
    {

        if (SceneManager.GetActiveScene().name == "RecordingPage")
        {
            string json = JsonUtility.ToJson(frames, true);
            writer.Write(json);
        }

        detecter.Dispose();
        if (SceneManager.GetActiveScene().name == "RecordingPage")
        {
            writer.Close();
            if (new FileInfo(filePath).Length == 1)
            {
                File.Delete(filePath);
                try
                {
                    Directory.Delete(dirPath);
                }
                catch { }
            }
        }
    }
    void CreateDataFile()
    {
        dirPath = Application.dataPath + "/Conduction/Data/" + MainManager.Instance.dirPath;

        if (!Directory.Exists(dirPath))
        {
            Directory.CreateDirectory(dirPath);
        }
        filePath = dirPath + "/data.json";
        writer = new StreamWriter(filePath, true);
    }

}
