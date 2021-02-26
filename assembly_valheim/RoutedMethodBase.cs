using System;

internal interface RoutedMethodBase
{
	void Invoke(long rpc, ZPackage pkg);
}
