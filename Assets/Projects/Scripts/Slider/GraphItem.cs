using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Kaimin.Managers;
using UnityEngine.UI;
using Graph;
using UniRx;

public class GraphItem : MonoBehaviour, IIbikiData, IBreathData, IHeadDirData, ISleepInfo
{

	public Image noDataImage = null;  //データがない場合に表示する画像
	public GameObject scrollView;

	public GraphDataSource graphDataSource;
	public IbikiGraph ibikiGraph;
	public BreathGraph breathGraph;

	/// <summary>
	/// グラフに表示するデータが変更された際に通知する
	/// </summary>
	public Subject<Unit> OnGraphDataChange = new Subject<Unit>();
	public bool isActive = false;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public SleepDataDetail GetSleepInfoData()
	{
		return graphDataSource.GetSleepInfoData();
	}

	public List<IbikiGraph.Data> GetIbikiDatas()
	{
		return graphDataSource.GetIbikiDatas();

	}

	public List<HeadDirGraph.Data> GetHeadDirDatas()
	{
		return graphDataSource.GetHeadDirDatas();
	}

	public List<BreathGraph.Data> GetBreathDatas()
	{
		return graphDataSource.GetBreathDatas();
	}
}
