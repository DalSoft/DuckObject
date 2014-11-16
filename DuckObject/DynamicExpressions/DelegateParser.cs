using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;
using System.Text;

namespace DalSoft.Dynamic.DynamicExpressions
{
        //Thanks and credit to http://www.codeproject.com/Articles/74018/How-to-Parse-and-Convert-a-Delegate-into-an-Expres
        internal class DelegateParser
        {
            internal DelegateParser()
            {
                LastNode = null;
            }
            internal DelegateNode LastNode { get; set; }

            public static DelegateNode Parse(Delegate func, params string[] tags)
            {
                if (func == null) throw new ArgumentNullException("Delegate");

                // Validating the delegate to parse...
                Type type = func.GetType();
                string name = type.Name;
                bool isFunc = name.StartsWith("Func", StringComparison.OrdinalIgnoreCase);
                bool isAction = name.StartsWith("Action", StringComparison.OrdinalIgnoreCase);
                if (!isFunc && !isAction) throw new InvalidOperationException("DelegateParser can only parse Action<> or Func<> delegates.");

                // Validating the expression has at least one dynamic argument...
                int num = type.GetGenericArguments().Length;
                if (isFunc) --num;
                if (num <= 0) throw new InvalidOperationException("DelegateParser needs at least one dynamic argument.");

                // Preparing the Parser object...
                DelegateParser parser = new DelegateParser();

                // Preparing the arguments...
                dynamic[] args = new DelegateNode[num];
                for (int i = 0; i < num; i++)
                {
                    string tag = (tags != null && i < tags.Length) ? tags[i] : ("T" + i.ToString());
                    args[i] = new DelegateNode(tag) { _parser = parser };
                }

                // Executing (so to speak) in order to perform the bindings and grab the latest node...
                object r = func.DynamicInvoke(args);

                // Returning...
                if (parser.LastNode != null) return parser.LastNode;
                if (r != null && r is DelegateNode) return (DelegateNode)r;
                return new DelegateNode.Value(r);
            }
            public static DelegateNode Parse(Delegate func)
            {
                return DelegateParser.Parse(func, null);
            }
        }

        internal class DelegateMetaNode : DynamicMetaObject
        {
            #region
            [Conditional("UNDER_CONSTRUCTION")]
            static public void TraceBind(string format, params object[] args) { Console.Write(format, args); }
            #endregion
            public DelegateMetaNode(Expression parameter, BindingRestrictions rest, object value) : base(parameter, rest, value) { }
            public override string ToString()
            {
                return Value == null ? base.ToString() : Value.ToString();
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                var @this = (DelegateNode)Value;
                var node = new DelegateNode.MemberGet(@this, binder.Name) { _parser = @this._parser };
                node._parser.LastNode = node;
                TraceBind("GETMEMBER: {0}\n", node);

                var par = Expression.Variable(typeof(DelegateNode.MemberGet), "ret");
                var exp = Expression.Block(
                    new ParameterExpression[] { par },
                    Expression.Assign(par, Expression.Constant(node))
                    );
                var restr = this.Restrictions;
                var ret = node;
                return new DelegateMetaNode(exp, restr, ret);
            }
            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                var @this = (DelegateNode)Value;
                var node = new DelegateNode.MemberSet(@this, binder.Name, value.Value) { _parser = @this._parser };
                node._parser.LastNode = node;
                TraceBind("SETMEMBER: {0}\n", node);

                var par = Expression.Variable(typeof(DelegateNode.MemberSet), "ret");
                var exp = Expression.Block(
                    new ParameterExpression[] { par },
                    Expression.Assign(par, Expression.Constant(node))
                    );
                var restr = this.Restrictions;
                var ret = node;
                return new DelegateMetaNode(exp, restr, ret);
            }

            public static string ObjectsToString(object[] objects, bool round)
            {
                StringBuilder sb = new StringBuilder(round ? "(" : "["); if (objects != null)
                {
                    bool first = true; foreach (object obj in objects)
                    {
                        if (!first) sb.Append(","); first = false;
                        object val = obj is DynamicMetaObject ? ((DynamicMetaObject)obj).Value : obj;
                        sb.AppendFormat("{0}", val == null ? "NULL" : val.ToString());
                    }
                }
                sb.Append(round ? ")" : "]"); return sb.ToString();
            }
            public static object[] DynamicToObjects(DynamicMetaObject[] metaObjects)
            {
                if (metaObjects == null) return null;

                List<object> list = new List<object>();
                foreach (DynamicMetaObject metaObject in metaObjects) list.Add(metaObject.Value);
                return list.ToArray();
            }

            public override DynamicMetaObject BindGetIndex(GetIndexBinder binder, DynamicMetaObject[] indexes)
            {
                var @this = (DelegateNode)Value;
                var node = new DelegateNode.IndexGet(@this, DynamicToObjects(indexes)) { _parser = @this._parser };
                node._parser.LastNode = node;
                TraceBind("GETINDEX: {0}\n", node);

                var par = Expression.Variable(typeof(DelegateNode.IndexGet), "ret");
                var exp = Expression.Block(
                    new ParameterExpression[] { par },
                    Expression.Assign(par, Expression.Constant(node))
                    );
                var restr = this.Restrictions;
                var ret = node;
                return new DelegateMetaNode(exp, restr, ret);
            }

            public override DynamicMetaObject BindSetIndex(SetIndexBinder binder, DynamicMetaObject[] indexes, DynamicMetaObject value)
            {
                var @this = (DelegateNode)Value;
                var node = new DelegateNode.IndexSet(@this, DynamicToObjects(indexes), value.Value) { _parser = @this._parser };
                node._parser.LastNode = node;
                TraceBind("SETINDEX: {0}\n", node);

                var par = Expression.Variable(typeof(DelegateNode.IndexSet), "ret");
                var exp = Expression.Block(
                    new ParameterExpression[] { par },
                    Expression.Assign(par, Expression.Constant(node))
                    );
                var restr = this.Restrictions;
                var ret = node;
                return new DelegateMetaNode(exp, restr, ret);
            }
            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                var @this = (DelegateNode)Value;
                var node = new DelegateNode.Method(@this, binder.Name, DynamicToObjects(args)) { _parser = @this._parser };
                node._parser.LastNode = node;
                TraceBind("METHOD: {0}\n", node);

                var par = Expression.Variable(typeof(DelegateNode.Method), "ret");
                var exp = Expression.Block(
                    new ParameterExpression[] { par },
                    Expression.Assign(par, Expression.Constant(node))
                    );
                var restr = this.Restrictions;
                var ret = node;
                return new DelegateMetaNode(exp, restr, ret);
            }

            public override DynamicMetaObject BindInvoke(InvokeBinder binder, DynamicMetaObject[] args)
            {
                var @this = (DelegateNode)Value;
                var node = new DelegateNode.Invoke(@this, DynamicToObjects(args)) { _parser = @this._parser };
                node._parser.LastNode = node;
                TraceBind("INVOKE: {0}\n", node);

                var par = Expression.Variable(typeof(DelegateNode.Invoke), "ret");
                var exp = Expression.Block(
                    new ParameterExpression[] { par },
                    Expression.Assign(par, Expression.Constant(node))
                    );
                var restr = this.Restrictions;
                var ret = node;
                return new DelegateMetaNode(exp, restr, ret);
            }
            public override DynamicMetaObject BindConvert(ConvertBinder binder)
            {
                var @this = (DelegateNode)Value;
                var node = new DelegateNode.Convert(@this, binder.ReturnType) { _parser = @this._parser };
                node._parser.LastNode = node;
                TraceBind("CONVERTTO: {0}\n", node);

                var par = Expression.Variable(binder.ReturnType, "ret");
                object ret = null; try { ret = Activator.CreateInstance(binder.ReturnType, true); }
                catch
                {
                    if (binder.ReturnType == typeof(String)) ret = String.Empty;
                    else ret = new Object();
                }
                var exp = Expression.Block(
                    new ParameterExpression[] { par },
                    Expression.Assign(par, Expression.Constant(ret))
                    );
                var restr = this.Restrictions;
                return new DelegateMetaNode(exp, restr, node);
            }
            public override DynamicMetaObject BindBinaryOperation(BinaryOperationBinder binder, DynamicMetaObject arg)
            {
                var @this = (DelegateNode)Value;
                var node = new DelegateNode.Binary(@this, binder.Operation, arg.Value) { _parser = @this._parser };
                node._parser.LastNode = node;
                TraceBind("BINARY: {0}\n", node);

                var par = Expression.Variable(typeof(DelegateNode.Binary), "ret");
                var exp = Expression.Block(
                    new ParameterExpression[] { par },
                    Expression.Assign(par, Expression.Constant(node))
                    );
                var restr = this.Restrictions;
                var ret = node;
                return new DelegateMetaNode(exp, restr, ret);
            }
            public override DynamicMetaObject BindUnaryOperation(UnaryOperationBinder binder)
            {
                var @this = (DelegateNode)Value;
                var node = new DelegateNode.Unary(@this, binder.Operation) { _parser = @this._parser };
                node._parser.LastNode = node;
                TraceBind("UNARY: {0}\n", node);

                if (binder.Operation == ExpressionType.IsTrue || binder.Operation == ExpressionType.IsFalse)
                {
                    var par = Expression.Variable(typeof(bool), "ret");
                    var exp = Expression.Block(
                        new ParameterExpression[] { par },
                        Expression.Assign(par, Expression.Constant(false))
                        );
                    var restr = this.Restrictions;
                    var ret = node;
                    return new DelegateMetaNode(exp, restr, ret);
                }
                else
                {
                    var par = Expression.Variable(typeof(DelegateNode.Unary), "ret");
                    var exp = Expression.Block(
                        new ParameterExpression[] { par },
                        Expression.Assign(par, Expression.Constant(node))
                        );
                    var restr = this.Restrictions;
                    var ret = node;
                    return new DelegateMetaNode(exp, restr, ret);
                }
            }
        }

        internal class DelegateNode : IDynamicMetaObjectProvider
        {
            internal DelegateParser _parser = null;
            public DelegateNode()
            {
                NodeName = null;
                NodeParent = null;
            }
            public DelegateNode(string name)
                : this()
            {
                NodeName = name.Validated("Node name", invalidChars: TypeHelper.InvalidMemberNameChars);
            }

            public string NodeName
            {
                get;
                private set;
            }
            public DelegateNode NodeParent
            {
                get;
                private set;
            }
            public int NodeLevel()
            {
                if (NodeParent == null) return 0;
                return NodeParent.NodeLevel() + 1;
            }

            public override string ToString()
            {
                return NodeName;
            }
            public DynamicMetaObject GetMetaObject(Expression parameter)
            {
                DynamicMetaObject meta = new DelegateMetaNode(
                    parameter,
                    BindingRestrictions.GetInstanceRestriction(parameter, this),
                    this);
                return meta;
            }

            public class Value : DelegateNode
            {
                public Value(object value, string name = null)
                    : base()
                {
                    if (name != null) NodeName = name.Validated("Node name", invalidChars: TypeHelper.InvalidMemberNameChars);
                    NodeValue = value;
                }
                public object NodeValue
                {
                    get;
                    private set;
                }
                public override string ToString()
                {
                    StringBuilder sb = new StringBuilder();
                    if (NodeName != null) sb.AppendFormat("({0}=", NodeName);
                    sb.Append(NodeValue == null ? "NULL" : NodeValue.ToString());
                    if (NodeName != null) sb.Append(")");
                    return sb.ToString();
                }
            }

            public class MemberGet : DelegateNode
            {
                public MemberGet(DelegateNode parent, string name)
                    : base(name)
                {
                    if ((NodeParent = parent) == null) throw new ArgumentNullException("Parent");
                }
                public override string ToString()
                {
                    return string.Format("{0}.{1}", NodeParent.ToString(), NodeName);
                }
            }
            public class MemberSet : DelegateNode
            {
                public MemberSet(DelegateNode parent, string name, object value)
                    : base(name)
                {
                    if ((NodeParent = parent) == null) throw new ArgumentNullException("Parent");
                    NodeValue = value;
                }
                public object NodeValue
                {
                    get;
                    private set;
                }
                public override string ToString()
                {
                    return string.Format("({0}.{1}={2})", NodeParent.ToString(), NodeName, NodeValue == null ? "NULL" : NodeValue.ToString());
                }
            }
            public class IndexGet : DelegateNode
            {
                public IndexGet(DelegateNode parent, object[] indexes)
                    : base()
                {
                    if ((NodeParent = parent) == null) throw new ArgumentNullException("Parent");
                    NodeIndexes = indexes;
                }
                public object[] NodeIndexes
                {
                    get;
                    private set;
                }
                public override string ToString()
                {
                    return string.Format("{0}{1}", NodeParent.ToString(), DelegateMetaNode.ObjectsToString(NodeIndexes, false));
                }
            }
            public class IndexSet : DelegateNode
            {
                public IndexSet(DelegateNode parent, object[] indexes, object value)
                    : base()
                {
                    if ((NodeParent = parent) == null) throw new ArgumentNullException("Parent");
                    NodeIndexes = indexes;
                    NodeValue = value;
                }
                public object[] NodeIndexes
                {
                    get;
                    private set;
                }
                public object NodeValue
                {
                    get;
                    private set;
                }
                public override string ToString()
                {
                    return string.Format("({0}{1}={2})", NodeParent.ToString(), DelegateMetaNode.ObjectsToString(NodeIndexes, false), NodeValue == null ? "NULL" : NodeValue.ToString());
                }
            }

            public class Method : DelegateNode
            {
                public Method(DelegateNode parent, string name, object[] arguments)
                    : base(name)
                {
                    if ((NodeParent = parent) == null) throw new ArgumentNullException("Parent");
                    NodeArguments = arguments;
                }
                public object[] NodeArguments
                {
                    get;
                    private set;
                }
                public override string ToString()
                {
                    return string.Format("{0}.{1}{2}", NodeParent.ToString(), NodeName, DelegateMetaNode.ObjectsToString(NodeArguments, true));
                }
            }
            public class Invoke : DelegateNode
            {
                public Invoke(DelegateNode target, object[] arguments)
                    : base()
                {
                    if ((NodeTarget = target) == null) throw new ArgumentNullException("Target");
                    NodeArguments = arguments;
                }
                public DelegateNode NodeTarget
                {
                    get;
                    private set;
                }
                public object[] NodeArguments
                {
                    get;
                    private set;
                }
                public override string ToString()
                {
                    return string.Format("{0}{1}", NodeName, DelegateMetaNode.ObjectsToString(NodeArguments, true));
                }
            }

            public class Binary : DelegateNode
            {
                public Binary(DelegateNode left, ExpressionType operation, object right)
                    : base()
                {
                    if ((NodeLeft = left) == null) throw new ArgumentNullException("Left operand");
                    NodeOperation = operation;
                    NodeRight = right;
                }
                public DelegateNode NodeLeft
                {
                    get;
                    private set;
                }
                public object NodeRight
                {
                    get;
                    private set;
                }
                public ExpressionType NodeOperation
                {
                    get;
                    private set;
                }
                public override string ToString()
                {
                    return string.Format("({0} {1} {2})", NodeLeft.ToString(), NodeOperation, NodeRight == null ? "null" : NodeRight.ToString());
                }
            }
            public class Unary : DelegateNode
            {
                public Unary(DelegateNode target, ExpressionType operation)
                    : base()
                {
                    if ((NodeTarget = target) == null) throw new ArgumentNullException("Target operand");
                    NodeOperation = operation;
                }
                public DelegateNode NodeTarget
                {
                    get;
                    private set;
                }
                public ExpressionType NodeOperation
                {
                    get;
                    private set;
                }
                public override string ToString()
                {
                    return string.Format("({0}::{1})", NodeOperation, NodeTarget.ToString());
                }
            }
            public class Convert : DelegateNode
            {
                public Convert(DelegateNode target, Type type)
                    : base()
                {
                    if ((NodeTarget = target) == null) throw new ArgumentNullException("Target operand");
                    if ((NodeType = type) == null) throw new ArgumentNullException("Conversion Type");
                }
                public DelegateNode NodeTarget
                {
                    get;
                    private set;
                }
                public Type NodeType
                {
                    get;
                    private set;
                }
                public override string ToString()
                {
                    return string.Format("({0}::{1})", NodeType, NodeTarget.ToString());
                }
            }
        }
    }
