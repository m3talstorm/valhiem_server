using System;

internal class RoutedMethod<T, U, V> : RoutedMethodBase
{
	public RoutedMethod(Action<long, T, U, V> action)
	{
		this.m_action = action;
	}

	public void Invoke(long rpc, ZPackage pkg)
	{
		this.m_action.DynamicInvoke(ZNetView.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
	}

	private Action<long, T, U, V> m_action;
}
