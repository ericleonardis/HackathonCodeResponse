using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;
using Boomlagoon.JSON;
using System.Collections.Concurrent;
using System.Threading;

public class threader
{
	public static Process process;
    public static void KillProcess()
    {
        process.Kill();
    }
    public static ConcurrentQueue<string> outMessages;
    public static ConcurrentQueue<string> inMessages;
    public static void talkToPython()
    {
	    process = new Process 
	    {
		    StartInfo = new ProcessStartInfo
		    {
			    FileName = @"C:\Users\TDA\Miniconda2\envs\ibm\python.exe",
				     Arguments = "1337.py",
				     UseShellExecute = false,
				     RedirectStandardInput = true,
				     RedirectStandardOutput = true,
				     CreateNoWindow = true
		    }
	    };
	    process.Start();
	    while (true){
		    Thread.Sleep(500);
		    string message;
		    if (threader.inMessages.Count>0){
			    if (threader.inMessages.TryDequeue(out message)){
				    process.StandardInput.WriteLine(message);
				    process.StandardInput.Flush();
				    string line = process.StandardOutput.ReadLine();
				    outMessages.Enqueue(line);
			    }
		    }
	    }

    }
}
public class SendWebCamData : MonoBehaviour {
    void OnApplicationQuit()
    {
        UnityEngine.Debug.Log("Quitting!");
        threader.KillProcess();
    }
	public TextMesh peopleInBuildingText;
	public WebCamTexture webCamTex;
	public WebCamTexture webCamTex2;
	public Texture2D rt;
	public Texture2D rt2;
	private Color32[] colors;
	private Color32[] colors2;
	public Material screenMat;
	public Material screenMat2;
	public int peopleInBuilding;
	void Start () {
		threader.outMessages = new ConcurrentQueue<string>();
		threader.inMessages = new ConcurrentQueue<string>();
		ThreadStart start = new ThreadStart(threader.talkToPython);
		Thread t = new Thread(start);
		t.Start();
		//processInfo.OutputDataRecieved += SortOutputHandler;
		//processInfo.Start();
        	webCamTex = new WebCamTexture(WebCamTexture.devices[0].name);
        	webCamTex2 = new WebCamTexture(WebCamTexture.devices[1].name);
        	//webCamTex2 = new WebCamTexture(WebCamTexture.devices[2].name);
		UnityEngine.Debug.Log(WebCamTexture.devices[0].name);
		UnityEngine.Debug.Log(WebCamTexture.devices[1].name);
        	webCamTex.Play();
        	webCamTex2.Play();
		rt = new Texture2D(webCamTex.width,webCamTex.height);
		rt2 = new Texture2D(webCamTex2.width,webCamTex2.height);
		screenMat.SetTexture("_MainTex",rt);
		screenMat2.SetTexture("_MainTex",rt2);
		client = new HttpClient();
		client2 = new HttpClient();
		imageIndex=0;
		imageIndex2=0;
	}

	
	public bool takeSnap=false;
	//private ProcessStartInfo StartInfo;
	private HttpClient client;
	private HttpClient client2;
	byte[] bytes;
	byte[] bytes2;
	int imageIndex=0;
	int imageIndex2=0;
	private float timeSinceLastPicture=0;
	public float timeBetweenPictures=3;

	void cropImage(int startX,int startY,int width, int height,int index,int faceCounter,bool isLeaving){
		//var tex = new Texture2D(4, 4, TextureFormat.DXT1, false);
		var tex = new Texture2D(4, 4);
        //C:\Users\TDA\Documents\Python\IBM RescueFace\imgs
        WWW www = new WWW("file:///C://Users//TDA//Documents//Python//IBMRescueFace//FaceCapture"+(isLeaving?"Sub":"Add")+index+".png");
        www.LoadImageIntoTexture(tex);
        /*int centerX = startX + width / 2;
        int centerY = startY + height / 2;
        if (centerX < 50 || centerX > width - 50 || centerY < 50 || centerY > height - 50)
        {
            UnityEngine.Debug.Log("ON EDGE!!");
            return;
        }*/
		if (startY+2*height>=tex.height){
			UnityEngine.Debug.Log("Face too low to get shirt data");
			return;
		}
		Color[] crop = tex.GetPixels(startX,tex.height-startY-height,width,height);
		Color[] shirtCrop = tex.GetPixels(startX,tex.height-startY-2*height,width,height);
		var newTex = new Texture2D(width,height);
		var shirtTex = new Texture2D(width,height);
		newTex.SetPixels(crop);
		shirtTex.SetPixels(shirtCrop);
		TextureScale.Bilinear(newTex,224,224);
        	byte[] bytes = newTex.EncodeToJPG();
        	byte[] shirtBytes = shirtTex.EncodeToJPG();
		
        var CroppedName = "Cropped"+(isLeaving?"Sub":"Add")+index+"Face"+faceCounter+".jpg";
        var CroppedFacePath = Application.dataPath + "/../imgs/"+CroppedName;


        var ShirtName = "Shirt"+(isLeaving?"Sub":"Add")+index+"Face"+faceCounter+".jpg";
        var ShirtPath = Application.dataPath + "/../imgs/" + ShirtName;

        File.WriteAllBytes(CroppedFacePath, bytes);
		File.WriteAllBytes(ShirtPath, shirtBytes);
		var message = CroppedName+" "+ShirtName;
		threader.inMessages.Enqueue((isLeaving?"Sub ":"Add ")+message);
		
	}

	/*Color32 copyColors(Color32 array){
		var retr = new Color32[array.length];	
	}*/

	async void PostData(int index,bool isLeaving){
		var client = new HttpClient();
		//client.BaseAddress = new System.Uri("https://gateway.watsonplatform.net");
		var request = new HttpRequestMessage(HttpMethod.Post, "https://gateway.watsonplatform.net/visual-recognition/api/v3/detect_faces?version=2018-03-1");

		var byteArray = new UTF8Encoding().GetBytes("apikey:ZOrzfoyOJYQQtiewyE_iMxPCMRjVYWdnWNaJTo3D4xJP");
		client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", System.Convert.ToBase64String(byteArray));

		var fileBytes = System.IO.File.ReadAllBytes("FaceCapture"+(isLeaving?"Sub":"Add")+index+".png");
		var fileContent = new ByteArrayContent(fileBytes,0,fileBytes.Length);
		MultipartFormDataContent multipartContent = new MultipartFormDataContent();
		multipartContent.Add(fileContent,"images_file","FaceCapture"+(isLeaving?"Sub":"Add")+index+".png");
		HttpResponseMessage response = await client.PostAsync("https://gateway.watsonplatform.net/visual-recognition/api/v3/detect_faces?version=2018-03-19",multipartContent);
		HttpContent responseContent = response.Content;

		string JSON;
		// Get the stream of the content.
		using (var reader = new StreamReader(await responseContent.ReadAsStreamAsync()))
		{
			// Write the output.
			JSON = await reader.ReadToEndAsync();
			UnityEngine.Debug.Log(JSON);
		}
		var jsonObject = JSONObject.Parse(JSON);
		var images = jsonObject.GetArray("images");
		var faceCounter=0;
		foreach (var i in images){
			foreach (var j in JSONObject.Parse(i.ToString()).GetArray("faces")){
				faceCounter++;
				var face_location = JSONObject.Parse(j.ToString()).GetObject("face_location");
				cropImage((int)face_location.GetNumber("left"),(int)face_location.GetNumber("top"),(int)face_location.GetNumber("width"),(int)face_location.GetNumber("height"),index,faceCounter,isLeaving);
				//UnityEngine.Debug.Log(face_location);
			}
		}

	}

	void Update () {
		peopleInBuildingText.text = "People left in building: "+peopleInBuilding;
		timeSinceLastPicture+=Time.deltaTime;		
		string message;
		if (threader.outMessages.Count>0){
			if (threader.outMessages.TryDequeue(out message)){
				UnityEngine.Debug.Log(message);
				int i;
				if (int.TryParse(message,out i)){
					peopleInBuilding=i;
				}
			}
		}
		if (colors==null){
			colors = new Color32[webCamTex.width*webCamTex.height];
		}
		if (colors2==null){
			colors2 = new Color32[webCamTex2.width*webCamTex2.height];
		}
		webCamTex.GetPixels32(colors);
		webCamTex2.GetPixels32(colors2);
		rt.SetPixels32(colors);
		rt2.SetPixels32(colors2);
		rt.Apply(false);
		rt2.Apply(false);
        	byte[] bytes = rt.EncodeToPNG();
        	byte[] bytes2 = rt2.EncodeToPNG();
		if (timeSinceLastPicture>timeBetweenPictures){
			timeSinceLastPicture=0;
			//takeSnap=false;
			File.WriteAllBytes(Application.dataPath + "/../FaceCaptureAdd"+imageIndex+".png", bytes);
			File.WriteAllBytes(Application.dataPath + "/../FaceCaptureSub"+imageIndex2+".png", bytes2);
			PostData(imageIndex,false);
			PostData(imageIndex2,true);
			imageIndex++;
			imageIndex2++;
		}
	}
}
