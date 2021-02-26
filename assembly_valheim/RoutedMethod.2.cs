using System;

internal class RoutedMethod<T> : RoutedMethodBase
{
	public RoutedMethod(Action<long, T> action)
	{
		this.m_action = action;
	}

	public void Invoke(long rpc, ZPackage pkg)
	{
		this.m_action.DynamicInvoke(ZNetView.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
	}

	private Action<long, T> m_action;
}
