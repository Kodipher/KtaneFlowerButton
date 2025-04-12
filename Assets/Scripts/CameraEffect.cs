using UnityEngine;

// Original source: https://github.com/QuickzYT/camera-material-tutorial-source/blob/main/Assets/Scripts/CameraEffect.cs
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class CameraEffect : MonoBehaviour {

	public Material material;

	private void OnRenderImage(RenderTexture source, RenderTexture destination) {
		
		if (material == null) {
			Graphics.Blit(source, destination);
			return;
		}

		Graphics.Blit(source, destination, material);

	}

}
