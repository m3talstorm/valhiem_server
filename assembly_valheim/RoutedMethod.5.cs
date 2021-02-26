using System;

public class RoutedMethod<T, U, V, B> : RoutedMethodBase
{
	public RoutedMethod(RoutedMethod<T, U, V, B>.Method action)
	{
		this.m_action = action;
	}

	public void Invoke(long rpc, ZPackage pkg)
	{
		this.m_action.DynamicInvoke(ZNetView.Deserialize(rpc, this.m_action.Method.GetParameters(), pkg));
	}

	private RoutedMethod<T, U, V, B>.Method m_action;

	public delegate void Method(long sender, T p0, U p1, V p2, B p3);
}
