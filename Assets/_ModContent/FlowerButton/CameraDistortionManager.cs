using UnityEngine;


namespace FlowerButtonMod.FlowerButton {

	internal class CameraDistortionManager {

		#region //// Manager and Effect creation, destruction

		public CameraDistortionManager(Material distortionMaterial) {
			distortionMaterialTemplate = distortionMaterial;
		}

		readonly Material distortionMaterialTemplate;
		CameraEffect curentCameraEffect;

		public void AddDistortionToCamera() {
			if (curentCameraEffect != null) RemoveDistortionFromCamera();

			// Add and setup effect component
			curentCameraEffect = Camera.main.gameObject.AddComponent<CameraEffect>();
			curentCameraEffect.material = new Material(distortionMaterialTemplate);
		}

		public void RemoveDistortionFromCamera() {
			if (curentCameraEffect == null) return;
			Object.Destroy(curentCameraEffect);
			curentCameraEffect = null;
		}

		#endregion

		#region //// Distortion params

		public void AddTime(float time) {
			if (curentCameraEffect == null) return;

			float previous = curentCameraEffect.material.GetFloat("_DistortionTime");
			curentCameraEffect.material.SetFloat("_DistortionTime", previous + time);

			previous = curentCameraEffect.material.GetFloat("_TintTime");
			curentCameraEffect.material.SetFloat("_TintTime", previous + time);
		}

		public void AddDistortionTime(float time) {
			if (curentCameraEffect == null) return;

			float previous = curentCameraEffect.material.GetFloat("_DistortionTime");
			curentCameraEffect.material.SetFloat("_DistortionTime", previous + time);
		}

		/// <param name="stregnthUniform">In range of 0..1</param>
		public void SetTintStrength(float stregnthUniform) {
			if (curentCameraEffect == null) return;
			curentCameraEffect.material.SetFloat("_TintStrength", stregnthUniform);
		}

		/// <param name="stregnthUv">In uv space</param>
		public void SetDistortionStrengthX(float stregnthUv) {
			if (curentCameraEffect == null) return;
			curentCameraEffect.material.SetFloat("_DistortionStrengthX", stregnthUv);
		}

		public float DefaltMaxTintStrength => distortionMaterialTemplate.GetFloat("_TintStrength");
		
		public float DefaltMaxDistortionStrengthX => distortionMaterialTemplate.GetFloat("_DistortionStrengthX");

		#endregion

	}

}

