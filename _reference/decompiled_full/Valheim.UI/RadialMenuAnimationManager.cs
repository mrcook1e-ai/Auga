using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Valheim.UI;

internal class RadialMenuAnimationManager
{
	private class EndAction
	{
		private Action OnEnd;

		public string ID { get; private set; }

		public bool HasExecuted { get; private set; }

		public EndAction(string iD, Action onEnd)
		{
			ID = iD;
			HasExecuted = false;
			OnEnd = onEnd;
		}

		public void Execute()
		{
			OnEnd?.Invoke();
			HasExecuted = true;
		}

		public void Cancel()
		{
			HasExecuted = true;
		}
	}

	private abstract class Tween
	{
		public string ID { get; protected set; }

		public bool Active { get; protected set; }

		public bool Paused { get; protected set; }

		public bool HasEndAction { get; protected set; }

		public abstract void Start();

		public abstract void Tick(float deltaTime);

		public abstract void End();

		public void Pause()
		{
			Paused = true;
		}

		public void UnPause()
		{
			Paused = false;
		}

		public void Cancel()
		{
			Active = false;
		}
	}

	private abstract class TweenContainer<T> : Tween where T : struct
	{
		private bool _hasInitialized;

		protected T _targetValue;

		protected float _duration;

		protected float _elapsedTime;

		private Action OnEnd;

		private Action OnTick;

		protected Func<T> GetValue;

		private Action<T> SetValue;

		private void SharedInit(string id, T targetValue, float duration, Action onEnd, Action onTick = null)
		{
			base.ID = id;
			base.Active = false;
			_targetValue = targetValue;
			_duration = duration;
			_elapsedTime = 0f;
			OnEnd = onEnd;
			OnTick = onTick;
			_hasInitialized = true;
			base.HasEndAction = onEnd != null;
		}

		protected void InitInternal(string id, T targetValue, Func<T> get, Action<T> set, float duration = 1f, Action onEnd = null, Action onTick = null)
		{
			SharedInit(id, targetValue, duration, onEnd, onTick);
			GetValue = get;
			SetValue = set;
		}

		public override void Start()
		{
			if (!_hasInitialized)
			{
				Debug.LogError("Tween never initialized.");
			}
			else
			{
				base.Active = true;
			}
		}

		public override void Tick(float deltaTime)
		{
			if (!base.Active)
			{
				return;
			}
			if (_elapsedTime >= _duration)
			{
				End();
				OnEnd?.Invoke();
				return;
			}
			_elapsedTime = Mathf.Min(_elapsedTime + deltaTime, _duration);
			try
			{
				SetValue(UpdateValue());
				OnTick?.Invoke();
			}
			catch
			{
				base.Active = false;
			}
		}

		protected abstract T UpdateValue();

		public override void End()
		{
			try
			{
				SetValue(_targetValue);
				OnTick?.Invoke();
			}
			catch
			{
			}
			base.Active = false;
		}
	}

	private abstract class LerpContainer<T> : TweenContainer<T> where T : struct
	{
		private T _startValue;

		private float _alpha;

		protected Func<float, float> GetAlpha;

		private void LerpInit(T startVal, Func<float, float> easeFunction)
		{
			_startValue = startVal;
			_alpha = 0f;
			GetAlpha = easeFunction;
		}

		public void Init(string id, T targetValue, Func<T> get, Action<T> set, float duration = 1f, Func<float, float> easeFunction = null, Action onEnd = null, Action onTick = null)
		{
			InitInternal(id, targetValue, get, set, duration, onEnd, onTick);
			LerpInit(GetValue(), easeFunction);
		}

		public void Init(string id, T targetValue, T startValue, Action<T> set, float duration = 1f, Func<float, float> easeFunction = null, Action onEnd = null, Action onTick = null)
		{
			InitInternal(id, targetValue, null, set, duration, onEnd, onTick);
			LerpInit(startValue, easeFunction);
		}

		protected override T UpdateValue()
		{
			T result = Lerp(_startValue, _targetValue, _alpha);
			_alpha = Mathf.Clamp01(_elapsedTime / _duration);
			return result;
		}

		protected abstract T Lerp(T start, T end, float alpha);
	}

	private class FloatLerp : LerpContainer<float>
	{
		protected override float Lerp(float start, float end, float alpha)
		{
			return Mathf.Lerp(start, end, GetAlpha?.Invoke(alpha) ?? alpha);
		}
	}

	private class Vector2Lerp : LerpContainer<Vector2>
	{
		protected override Vector2 Lerp(Vector2 start, Vector2 end, float alpha)
		{
			return Vector2.Lerp(start, end, GetAlpha?.Invoke(alpha) ?? alpha);
		}
	}

	private class Vector3Lerp : LerpContainer<Vector3>
	{
		protected override Vector3 Lerp(Vector3 start, Vector3 end, float alpha)
		{
			return Vector3.Lerp(start, end, GetAlpha?.Invoke(alpha) ?? alpha);
		}
	}

	private class QuaternionLerp : LerpContainer<Quaternion>
	{
		protected override Quaternion Lerp(Quaternion start, Quaternion end, float alpha)
		{
			return Quaternion.Lerp(start, end, GetAlpha?.Invoke(alpha) ?? alpha);
		}
	}

	private class ColorLerp : LerpContainer<Color>
	{
		protected override Color Lerp(Color start, Color end, float alpha)
		{
			return Color.Lerp(start, end, GetAlpha?.Invoke(alpha) ?? alpha);
		}
	}

	private class AngleLerp : LerpContainer<float>
	{
		public void InitAngle(string id, float targetValue, float startValue, Action<float> set, float duration = 1f, Func<float, float> easeFunction = null, Action onEnd = null, Action onTick = null)
		{
			startValue = UIMath.Mod(startValue, 360f);
			if (Mathf.Abs(targetValue - startValue) > 180f)
			{
				targetValue = ((targetValue > startValue) ? (targetValue - 360f) : (targetValue + 360f));
			}
			Init(id, targetValue, startValue, set, duration, easeFunction, onEnd, onTick);
		}

		public void InitAngle(string id, float targetValue, Func<float> get, Action<float> set, float duration = 1f, Func<float, float> easeFunction = null, Action onEnd = null, Action onTick = null)
		{
			if (Mathf.Abs(targetValue - get()) > 180f)
			{
				targetValue = ((targetValue > get()) ? (targetValue - 360f) : (targetValue + 360f));
			}
			Init(id, targetValue, get, set, duration, easeFunction, onEnd, onTick);
		}

		protected override float Lerp(float start, float end, float alpha)
		{
			return UIMath.Mod(Mathf.Lerp(start, end, GetAlpha?.Invoke(alpha) ?? alpha), 360f);
		}
	}

	private abstract class DampContainer<T> : TweenContainer<T> where T : struct
	{
		protected T _velocity;

		protected float SmoothTime => Mathf.Max(_duration - _elapsedTime, 0.0001f);

		public void Init(string id, T targetValue, Func<T> get, Action<T> set, float duration = 1f, Action onEnd = null, Action onTick = null)
		{
			InitInternal(id, targetValue, get, set, duration, onEnd, onTick);
		}
	}

	private class FloatDamp : DampContainer<float>
	{
		public override void Start()
		{
			_velocity = 0f;
			base.Start();
		}

		protected override float UpdateValue()
		{
			return Mathf.SmoothDamp(GetValue(), _targetValue, ref _velocity, base.SmoothTime);
		}
	}

	private class Vector2Damp : DampContainer<Vector2>
	{
		public override void Start()
		{
			_velocity = Vector2.zero;
			base.Start();
		}

		protected override Vector2 UpdateValue()
		{
			return Vector2.SmoothDamp(GetValue(), _targetValue, ref _velocity, base.SmoothTime);
		}
	}

	private class Vector3Damp : DampContainer<Vector3>
	{
		public override void Start()
		{
			_velocity = Vector3.zero;
			base.Start();
		}

		protected override Vector3 UpdateValue()
		{
			return Vector3.SmoothDamp(GetValue(), _targetValue, ref _velocity, base.SmoothTime);
		}
	}

	private List<Tween> _activeTweens = new List<Tween>();

	private readonly Dictionary<string, EndAction> _endActions = new Dictionary<string, EndAction>();

	public bool IsTweenActive(string id)
	{
		foreach (Tween activeTween in _activeTweens)
		{
			if (activeTween.ID == id && activeTween.Active)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsTweenPaused(string id)
	{
		foreach (Tween activeTween in _activeTweens)
		{
			if (activeTween.ID == id && activeTween.Paused)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsTweenActiveWithEndAction(string id)
	{
		foreach (Tween activeTween in _activeTweens)
		{
			if (activeTween.ID == id && activeTween.Active && activeTween.HasEndAction)
			{
				return true;
			}
		}
		return false;
	}

	public void EndAll()
	{
		foreach (Tween activeTween in _activeTweens)
		{
			activeTween.End();
		}
		_activeTweens.Clear();
		foreach (EndAction value in _endActions.Values)
		{
			value.Cancel();
		}
		_endActions.Clear();
	}

	public void CancelAll()
	{
		foreach (Tween activeTween in _activeTweens)
		{
			activeTween.Cancel();
		}
		_activeTweens.Clear();
		foreach (EndAction value in _endActions.Values)
		{
			value.Cancel();
		}
		_endActions.Clear();
	}

	public void CancelTweens(string id)
	{
		CancelOrPauseTweens(id, delegate(Tween tween)
		{
			tween.Cancel();
		});
		if (_endActions.TryGetValue(id, out var value))
		{
			value.Cancel();
		}
	}

	public void EndTweens(string id)
	{
		CancelOrPauseTweens(id, delegate(Tween tween)
		{
			tween.End();
		});
		if (_endActions.TryGetValue(id, out var value))
		{
			value.Cancel();
		}
	}

	public void PauseTweens(string id)
	{
		CancelOrPauseTweens(id, delegate(Tween tween)
		{
			tween.Pause();
		});
	}

	public void UnPauseTweens(string id)
	{
		CancelOrPauseTweens(id, delegate(Tween tween)
		{
			tween.UnPause();
		});
	}

	private void CancelOrPauseTweens(string id, Action<Tween> action)
	{
		foreach (Tween item in _activeTweens.ToList())
		{
			if (!(item.ID != id))
			{
				action(item);
				if (!item.Active && !item.Paused)
				{
					_activeTweens.Remove(item);
				}
			}
		}
	}

	public void Tick(float deltaTime)
	{
		if (_activeTweens.Count <= 0)
		{
			return;
		}
		foreach (Tween item in _activeTweens.ToList())
		{
			if (item.Active && !item.Paused)
			{
				item.Tick(deltaTime);
			}
		}
		foreach (Tween item2 in _activeTweens.ToList())
		{
			if (!item2.Active)
			{
				_activeTweens.Remove(item2);
			}
		}
		foreach (EndAction a in _endActions.Values.ToList())
		{
			if (!a.HasExecuted && _activeTweens.All((Tween t) => t.ID != a.ID))
			{
				a.Execute();
			}
		}
		foreach (EndAction item3 in _endActions.Values.ToList())
		{
			if (item3.HasExecuted)
			{
				_endActions.Remove(item3.ID);
			}
		}
	}

	public void AddEnd(string id, Action onEnd)
	{
		foreach (Tween activeTween in _activeTweens)
		{
			if (!(activeTween.ID == id))
			{
				continue;
			}
			goto IL_004e;
		}
		Debug.LogWarning("No active tweens with ID: " + id);
		return;
		IL_004e:
		if (_endActions.ContainsKey(id))
		{
			_endActions.Remove(id);
		}
		_endActions.Add(id, new EndAction(id, onEnd));
	}

	public void StartUniqueTween<T>(Func<T> get, Action<T> set, string id, T targetValue, float duration = 1f, EasingType type = EasingType.Linear, Action onEnd = null, Action onTick = null) where T : struct
	{
		CancelTweens(id);
		StartTween(get, set, id, targetValue, duration, type, onEnd, onTick);
	}

	public void StartTween<T>(Func<T> get, Action<T> set, string id, T targetValue, float duration = 1f, EasingType type = EasingType.Linear, Action onEnd = null, Action onTick = null) where T : struct
	{
		Tween tween = ((type == EasingType.SmoothDamp) ? ((TweenContainer<T>)CreateDamp<T>()) : ((TweenContainer<T>)CreateLerp<T>()));
		if (tween != null)
		{
			if (tween is DampContainer<T> dampContainer)
			{
				dampContainer.Init(id, targetValue, get, set, duration, onEnd);
			}
			else if (tween is LerpContainer<T> lerpContainer)
			{
				lerpContainer.Init(id, targetValue, get, set, duration, EasingFunctions.GetFunc(type), onEnd, onTick);
			}
			_activeTweens.Add(tween);
			tween.Start();
		}
	}

	public void StartUniqueTween<T>(Action<T> set, string id, T startValue, T targetValue, float duration = 1f, EasingType type = EasingType.Linear, Action onEnd = null, Action onTick = null) where T : struct
	{
		CancelTweens(id);
		StartTween(set, id, startValue, targetValue, duration, type, onEnd, onTick);
	}

	public void StartTween<T>(Action<T> set, string id, T startValue, T targetValue, float duration = 1f, EasingType type = EasingType.Linear, Action onEnd = null, Action onTick = null) where T : struct
	{
		if (type == EasingType.SmoothDamp)
		{
			Debug.LogError("Easing Type 'SmoothDamp' requires a getter. Tween with ID \"" + id + "\" was called with a start value instead.");
			return;
		}
		LerpContainer<T> lerpContainer = CreateLerp<T>();
		if (lerpContainer != null)
		{
			lerpContainer.Init(id, targetValue, startValue, set, duration, EasingFunctions.GetFunc(type), onEnd, onTick);
			_activeTweens.Add(lerpContainer);
			lerpContainer.Start();
		}
	}

	public void StartUniqueAngleTween(Func<float> get, Action<float> set, string id, float targetValue, float duration = 1f, EasingType type = EasingType.Linear, Action onEnd = null, Action onTick = null)
	{
		CancelTweens(id);
		StartAngleTween(get, set, id, targetValue, duration, type, onEnd, onTick);
	}

	public void StartAngleTween(Func<float> get, Action<float> set, string id, float targetValue, float duration = 1f, EasingType type = EasingType.Linear, Action onEnd = null, Action onTick = null)
	{
		if (type == EasingType.SmoothDamp)
		{
			Debug.LogError("Smooth Damp currently not supported for angles.");
			return;
		}
		AngleLerp angleLerp = new AngleLerp();
		angleLerp.InitAngle(id, targetValue, get, set, duration, EasingFunctions.GetFunc(type), onEnd, onTick);
		_activeTweens.Add(angleLerp);
		angleLerp.Start();
	}

	public void StartUniqueAngleTween(Action<float> set, string id, float startValue, float targetValue, float duration = 1f, EasingType type = EasingType.Linear, Action onEnd = null, Action onTick = null)
	{
		CancelTweens(id);
		StartAngleTween(set, id, startValue, targetValue, duration, type, onEnd, onTick);
	}

	public void StartAngleTween(Action<float> set, string id, float startValue, float targetValue, float duration = 1f, EasingType type = EasingType.Linear, Action onEnd = null, Action onTick = null)
	{
		if (type == EasingType.SmoothDamp)
		{
			Debug.LogError("Smooth Damp currently not supported for angles.");
			return;
		}
		AngleLerp angleLerp = new AngleLerp();
		angleLerp.InitAngle(id, targetValue, startValue, set, duration, EasingFunctions.GetFunc(type), onEnd, onTick);
		_activeTweens.Add(angleLerp);
		angleLerp.Start();
	}

	private static LerpContainer<T> CreateLerp<T>() where T : struct
	{
		if (typeof(T) == typeof(float))
		{
			return new FloatLerp() as LerpContainer<T>;
		}
		if (typeof(T) == typeof(Vector2))
		{
			return new Vector2Lerp() as LerpContainer<T>;
		}
		if (typeof(T) == typeof(Vector3))
		{
			return new Vector3Lerp() as LerpContainer<T>;
		}
		if (typeof(T) == typeof(Quaternion))
		{
			return new QuaternionLerp() as LerpContainer<T>;
		}
		if (typeof(T) == typeof(Color))
		{
			return new ColorLerp() as LerpContainer<T>;
		}
		throw new InvalidOperationException("No lerp container defined for type " + typeof(T).Name);
	}

	private static DampContainer<T> CreateDamp<T>() where T : struct
	{
		if (typeof(T) == typeof(float))
		{
			return new FloatDamp() as DampContainer<T>;
		}
		if (typeof(T) == typeof(Vector2))
		{
			return new Vector2Damp() as DampContainer<T>;
		}
		if (typeof(T) == typeof(Vector3))
		{
			return new Vector3Damp() as DampContainer<T>;
		}
		throw new InvalidOperationException("No damp container defined for type " + typeof(T).Name);
	}
}
