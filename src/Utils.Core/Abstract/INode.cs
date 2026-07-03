namespace zms9110750.Utils.Core.Abstract;

/// <summary>
/// 树节点接口
/// </summary>
/// <typeparam name="TNode">自我约束</typeparam> 
public interface INode<TNode> where TNode : INode<TNode>
{
    /// <summary>
    /// 父节点
    /// </summary>
    TNode? Parent { get; }
    /// <summary>
    /// 根节点
    /// </summary>
    /// <remarks>
    /// 根节点的这个属性为自己
    /// </remarks>
    TNode Root { get; }
    /// <summary>
    /// 节点深度
    /// </summary>
    /// <remarks>
    /// 根节点的深度为0
    /// </remarks>
    int Depth { get; }
}
