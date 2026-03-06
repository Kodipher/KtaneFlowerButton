using UnityEngine;


namespace FlowerButtonMod.FlowerButton {

	internal class CameraDistortionManager {

		#region //// Manager and Effect creation, destruction

		public CameraDistortionManager(Material distortionMaterial) {
			distortionMaterialTemplate = distortionMaterial;
		}

		readonly Material distortionMaterialTemplate;
		CameraEffect currentCameraEffect;

		public void AddDistortionToCamera() {
			if (currentCameraEffect != null) RemoveDistortionFromCamera();

			// Add and setup effect component
			currentCameraEffect = Camera.main.gameObject.AddComponent<CameraEffect>();
			currentCameraEffect.material = new Material(distortionMaterialTemplate);
		}

		public void RemoveDistortionFromCamera() {
			if (currentCameraEffect == null) return;
			Object.Destroy(currentCameraEffect);
			currentCameraEffect = null;
		}

		#endregion

		#region //// Distortion params

		public void AddTime(float time) {
			if (currentCameraEffect == null) return;

			float previous = currentCameraEffect.material.GetFloat("_DistortionTime");
			currentCameraEffect.material.SetFloat("_DistortionTime", previous + time);

			previous = currentCameraEffect.material.GetFloat("_TintTime");
			currentCameraEffect.material.SetFloat("_TintTime", previous + time);
		}

		public void AddDistortionTime(float time) {
			if (currentCameraEffect == null) return;

			float previous = currentCameraEffect.material.GetFloat("_DistortionTime");
			currentCameraEffect.material.SetFloat("_DistortionTime", previous + time);
		}

		/// <param name="strengthUniform">In range of 0..1</param>
		public void SetTintStrength(float strengthUniform) {
			if (currentCameraEffect == null) return;
			currentCameraEffect.material.SetFloat("_TintStrength", strengthUniform);
		}

		/// <param name="strengthUv">In uv space</param>
		public void SetDistortionStrengthX(float strengthUv) {
			if (currentCameraEffect == null) return;
			currentCameraEffect.material.SetFloat("_DistortionStrengthX", strengthUv);
		}

		public float DefaultMaxTintStrength => distortionMaterialTemplate.GetFloat("_TintStrength");
		
		public float DefaultMaxDistortionStrengthX => distortionMaterialTemplate.GetFloat("_DistortionStrengthX");

		#endregion

	}

}

