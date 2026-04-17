using UnityEngine;

namespace Valheim.UI;

public class RadialDataInitializer : MonoBehaviour
{
	[SerializeReference]
	private RadialDataSO _dataObject;

	private void Awake()
	{
		RadialData.Init(_dataObject);
	}
}
