using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RayTracingMaster : MonoBehaviour
{
	public ComputeShader RayTracingShader;
	public Texture SkyboxTexture;
	public Light DirectionalLight;


	private RenderTexture _target;
	private Material _addMaterial;
	private uint _currentSample = 0;
	private Camera _camera;
	private ComputeBuffer _sphereBuffer;

	[Header("Spheres")]
	public Vector2 SphereRadius = new Vector2(3.0f, 8.0f);
	public uint SpheresMax = 100;
	public float SpherePlacementRadius = 100.0f;

	struct Sphere
	{
		public Vector3 position;
		public float radius;
		public Vector3 albedo;
		public Vector3 specular;
		public float refraction;
		public Vector3 refractionColor;
	}

	private void Awake()
	{
		_camera = GetComponent<Camera>();
	}

	private void OnEnable()
	{
		_currentSample = 0;
		SetUpScene();
	}

	private void OnDisable()
	{
		if (_sphereBuffer != null)
			_sphereBuffer.Release();
	}


	//private void SetUpScene()
	//{
	//	List<Sphere> spheres = new List<Sphere>();

	//	// Add a number of random spheres
	//	for (int i = 0; i < SpheresMax; i++)
	//	{
	//		Sphere sphere = new Sphere();

	//		// Radius and radius
	//		sphere.radius = SphereRadius.x + Random.value * (SphereRadius.y - SphereRadius.x);
	//		Vector2 randomPos = Random.insideUnitCircle * SpherePlacementRadius;
	//		sphere.position = new Vector3(randomPos.x, sphere.radius, randomPos.y);

	//		// Reject spheres that are intersecting others
	//		foreach (Sphere other in spheres)
	//		{
	//			float minDist = sphere.radius + other.radius;
	//			if (Vector3.SqrMagnitude(sphere.position - other.position) < minDist * minDist)
	//				goto SkipSphere;
	//		}

	//		// Albedo and specular color
	//		Color color = Random.ColorHSV();
	//		bool metal = Random.value < 0.5f;
	//		sphere.albedo = metal ? Vector4.zero : new Vector4(color.r, color.g, color.b);
	//		sphere.specular = metal ? new Vector4(color.r, color.g, color.b) : new Vector4(0.04f, 0.04f, 0.04f);

	//		Color refraction_color = Random.ColorHSV();
	//		sphere.refractionColor = new Vector4(refraction_color.r, refraction_color.g, refraction_color.b);
	//		sphere.refraction = Random.value;

	//		// Add the sphere to the list
	//		spheres.Add(sphere);

	//	SkipSphere:
	//		continue;
	//	}

	//	// Assign to compute buffer
	//	if (_sphereBuffer != null)
	//		_sphereBuffer.Release();
	//	if (spheres.Count > 0)
	//	{
	//		_sphereBuffer = new ComputeBuffer(spheres.Count, 40);
	//		_sphereBuffer.SetData(spheres);
	//	}
	//}


	private void SetUpScene()
	{
		List<Sphere> spheres = new List<Sphere>();

		Color color = Color.white;//Random.ColorHSV();
		Color refraction_color = Color.white;//Random.ColorHSV();
		int sep = 15;

		// Add a number of random spheres
		int i = 1;
		int j = 1;
		for (i = -3; i < 3; i++)
		{
			for (j = -3; j < 4; j++)
			{
				Sphere sphere = new Sphere();

				// Radius and radius
				sphere.radius = 10;
				sphere.position = new Vector3(i * (sphere.radius + sep), sphere.radius + 0.5f, j * (sphere.radius + sep));

				// Albedo and specular color				
				sphere.albedo = new Vector4(color.r, color.g, color.b);
				sphere.specular = new Vector4( 1,1,1) * (i+3.0f)/6.0f;

				sphere.refractionColor = new Vector4(refraction_color.r, refraction_color.g, refraction_color.b);
				sphere.refraction = (j + 3.0f) / 6.0f;

				// Add the sphere to the list
				spheres.Add(sphere);
			
			}
		}

		// Assign to compute buffer
		if (_sphereBuffer != null)
			_sphereBuffer.Release();
		if (spheres.Count > 0)
		{
			_sphereBuffer = new ComputeBuffer(spheres.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(Sphere)));
			_sphereBuffer.SetData(spheres);
		}
	}

	private void SetShaderParameters(){
		RayTracingShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
		RayTracingShader.SetMatrix("_CameraInverseProjection", _camera.projectionMatrix.inverse);
		RayTracingShader.SetTexture(0, "_SkyboxTexture", SkyboxTexture);

		Vector3 l = DirectionalLight.transform.forward;
		RayTracingShader.SetVector("_DirectionalLight", new Vector4(l.x, l.y, l.z, DirectionalLight.intensity));


		RayTracingShader.SetVector("_PixelOffset", new Vector2(Random.value, Random.value));

		if (_sphereBuffer != null)
			RayTracingShader.SetBuffer(0, "_Spheres", _sphereBuffer);

		// Set the target and dispatch the compute shader
		RayTracingShader.SetTexture(0, "Result", _target);
	}


	private void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		Render(destination);
	}
	private void Render(RenderTexture destination)
	{
		// Make sure we have a current render target
		InitRenderTexture();

		SetShaderParameters();

		
		int threadGroupsX = Mathf.CeilToInt(Screen.width / 8.0f);
		int threadGroupsY = Mathf.CeilToInt(Screen.height / 8.0f);
		RayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);
		// Blit the result texture to the screen
		if (_addMaterial == null)
			_addMaterial = new Material(Shader.Find("Hidden/AddShader"));
		_addMaterial.SetFloat("_Sample", _currentSample);
		Graphics.Blit(_target, destination, _addMaterial);
		_currentSample++;
	}
	private void InitRenderTexture()
	{
		if (_target == null || _target.width != Screen.width || _target.height != Screen.height)
		{
			// Release render texture if we already have one
			if (_target != null)
				_target.Release();
			// Get a render target for Ray Tracing
			_target = new RenderTexture(Screen.width, Screen.height, 0,
				RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
			_target.enableRandomWrite = true;
			_target.Create();

			// Reset sampling
			_currentSample = 0;
		}
	}


	private void Update()
	{
		if (transform.hasChanged)
		{
			_currentSample = 0;
			transform.hasChanged = false;
		}
	}

}