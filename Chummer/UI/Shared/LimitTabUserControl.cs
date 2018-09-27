/*  This file is part of Chummer5a.
 *
 *  Chummer5a is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Chummer5a is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with Chummer5a.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  You can obtain the full source code for Chummer5a at
 *  https://github.com/chummer5a/chummer5a
 */
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chummer.UI.Shared
{
    public partial class LimitTabUserControl : UserControl
    {
        private Character _characterObject;
        public event PropertyChangedEventHandler MakeDirty;
        public event PropertyChangedEventHandler MakeDirtyWithCharacterUpdate;

        public LimitTabUserControl()
        {
            InitializeComponent();
        }

        private void LimitTabUserControl_Load(object sender, EventArgs e)
        {
            if (_characterObject != null) return;
            if (ParentForm != null)
                ParentForm.Cursor = Cursors.WaitCursor;
            RealLoad();
            if (ParentForm != null)
                ParentForm.Cursor = Cursors.Default;
        }

        public void RealLoad()
        {
            if (ParentForm is CharacterShared frmParent)
                _characterObject = frmParent.CharacterObject;
            else
            {
                Utils.BreakIfDebug();
                _characterObject = new Character();
            }

            Utils.DoDatabinding(lblPhysical, "Text", _characterObject, nameof(Character.LimitPhysical));
            Utils.DoDatabinding(lblPhysical, "ToolTipText", _characterObject, nameof(Character.LimitPhysicalToolTip));
            Utils.DoDatabinding(lblMental, "Text", _characterObject, nameof(Character.LimitMental));
            Utils.DoDatabinding(lblMental, "ToolTipText", _characterObject, nameof(Character.LimitMentalToolTip));
            Utils.DoDatabinding(lblSocial, "Text", _characterObject, nameof(Character.LimitSocial));
            Utils.DoDatabinding(lblSocial, "ToolTipText", _characterObject, nameof(Character.LimitSocialToolTip));
            Utils.DoDatabinding(lblAstral, "Text", _characterObject, nameof(Character.LimitAstral));
            Utils.DoDatabinding(lblAstral, "ToolTipText", _characterObject, nameof(Character.LimitAstralToolTip));

            _characterObject.LimitModifiers.CollectionChanged += LimitModifierCollectionChanged;
        }
        #region Click Events
        private void cmdAddLimitModifier_Click(object sender, EventArgs e)
        {
            frmSelectLimitModifier frmPickLimitModifier = new frmSelectLimitModifier(null, "Physical", "Mental", "Social");
            frmPickLimitModifier.ShowDialog(this);

            if (frmPickLimitModifier.DialogResult == DialogResult.Cancel)
                return;

            // Create the new limit modifier.
            LimitModifier objLimitModifier = new LimitModifier(_characterObject);
            objLimitModifier.Create(frmPickLimitModifier.SelectedName, frmPickLimitModifier.SelectedBonus, frmPickLimitModifier.SelectedLimitType, frmPickLimitModifier.SelectedCondition);
            if (objLimitModifier.InternalId.IsEmptyGuid())
                return;

            _characterObject.LimitModifiers.Add(objLimitModifier);
            MakeDirtyWithCharacterUpdate?.Invoke(null, null);
        }

        private void cmdDeleteLimitModifier_Click(object sender, EventArgs e)
        {
            if (!(treLimit.SelectedNode?.Tag is ICanRemove selectedObject)) return;
            if (!selectedObject.Remove(_characterObject, _characterObject.Options.ConfirmDelete)) return;
            MakeDirtyWithCharacterUpdate?.Invoke(null, null);
        }
        private void treLimit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
            {
                cmdDeleteLimitModifier_Click(sender, e);
            }
        }

        private void tssLimitModifierNotes_Click(object sender, EventArgs e)
        {
            if (treLimit.SelectedNode == null) return;
            if (treLimit.SelectedNode?.Tag is IHasNotes objNotes)
            {
                
                WriteNotes(objNotes, treLimit.SelectedNode);
            }
            else
            {
                // the limit modifier has a source
                foreach (Improvement objImprovement in _characterObject.Improvements)
                {
                    if (objImprovement.ImproveType != Improvement.ImprovementType.LimitModifier ||
                        objImprovement.SourceName != treLimit.SelectedNode?.Tag.ToString()) continue;
                    string strOldValue = objImprovement.Notes;
                    frmNotes frmItemNotes = new frmNotes
                    {
                        Notes = strOldValue
                    };
                    frmItemNotes.ShowDialog(this);

                    if (frmItemNotes.DialogResult != DialogResult.OK) continue;
                    objImprovement.Notes = frmItemNotes.Notes;
                    if (objImprovement.Notes == strOldValue) continue;
                    MakeDirty?.Invoke(null, null);

                    treLimit.SelectedNode.ForeColor = objImprovement.PreferredColor;
                    treLimit.SelectedNode.ToolTipText = objImprovement.Notes.WordWrap(100);
                }
            }
        }

        private void tssLimitModifierEdit_Click(object sender, EventArgs e)
        {
            UpdateLimitModifier();
        }
        #endregion
        #region Methods

        /// <summary>
        /// Allows the user to input notes that should be linked to the selected object.
        /// TODO: Should be linked back to CharacterShared in some way or moved into a more generic helper class. 
        /// </summary>
        /// <param name="objNotes"></param>
        /// <param name="treNode"></param>
        protected void WriteNotes(IHasNotes objNotes, TreeNode treNode)
        {
            string strOldValue = objNotes.Notes;
            frmNotes frmItemNotes = new frmNotes
            {
                Notes = strOldValue
            };
            frmItemNotes.ShowDialog(this);

            if (frmItemNotes.DialogResult != DialogResult.OK) return;
            objNotes.Notes = frmItemNotes.Notes;
            if (objNotes.Notes == strOldValue) return;
            treNode.ForeColor = objNotes.PreferredColor;
            treNode.ToolTipText = objNotes.Notes.WordWrap(100);
            MakeDirty?.Invoke(null,null);
        }

        protected void RefreshLimitModifiers(NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs = null)
        {
            string strSelectedId = (treLimit.SelectedNode?.Tag as IHasInternalId)?.InternalId ?? string.Empty;

            TreeNode[] aobjLimitNodes = new TreeNode[(int)LimitType.NumLimitTypes];

            if (notifyCollectionChangedEventArgs == null)
            {
                treLimit.Nodes.Clear();

                // Add Limit Modifiers.
                foreach (LimitModifier objLimitModifier in _characterObject.LimitModifiers)
                {
                    int intTargetLimit = (int)Enum.Parse(typeof(LimitType), objLimitModifier.Limit);
                    TreeNode objParentNode = GetLimitModifierParentNode(intTargetLimit);
                    if (!objParentNode.Nodes.ContainsKey(objLimitModifier.DisplayName))
                    {
                        objParentNode.Nodes.Add(objLimitModifier.CreateTreeNode(cmsLimitModifier));
                    }
                }

                // Add Limit Modifiers from Improvements
                foreach (Improvement objImprovement in _characterObject.Improvements.Where(objImprovement => objImprovement.ImproveSource == Improvement.ImprovementSource.Custom))
                {
                    int intTargetLimit = -1;
                    switch (objImprovement.ImproveType)
                    {
                        case Improvement.ImprovementType.LimitModifier:
                            intTargetLimit = (int)Enum.Parse(typeof(LimitType), objImprovement.ImprovedName);
                            break;
                        case Improvement.ImprovementType.PhysicalLimit:
                            intTargetLimit = (int)LimitType.Physical;
                            break;
                        case Improvement.ImprovementType.MentalLimit:
                            intTargetLimit = (int)LimitType.Mental;
                            break;
                        case Improvement.ImprovementType.SocialLimit:
                            intTargetLimit = (int)LimitType.Social;
                            break;
                    }
                    if (intTargetLimit != -1)
                    {
                        TreeNode objParentNode = GetLimitModifierParentNode(intTargetLimit);
                        string strName = objImprovement.UniqueName + ": ";
                        if (objImprovement.Value > 0)
                            strName += '+';
                        strName += objImprovement.Value.ToString();
                        if (!string.IsNullOrEmpty(objImprovement.Condition))
                            strName += ", " + objImprovement.Condition;
                        if (!objParentNode.Nodes.ContainsKey(strName))
                        {
                            TreeNode objNode = new TreeNode
                            {
                                Name = strName,
                                Text = strName,
                                Tag = objImprovement.SourceName,
                                ContextMenuStrip = cmsLimitModifier,
                                ForeColor = objImprovement.PreferredColor,
                                ToolTipText = objImprovement.Notes.WordWrap(100)
                            };
                            if (string.IsNullOrEmpty(objImprovement.ImprovedName))
                            {
                                if (objImprovement.ImproveType == Improvement.ImprovementType.SocialLimit)
                                    objImprovement.ImprovedName = "Social";
                                else if (objImprovement.ImproveType == Improvement.ImprovementType.MentalLimit)
                                    objImprovement.ImprovedName = "Mental";
                                else
                                    objImprovement.ImprovedName = "Physical";
                            }

                            objParentNode.Nodes.Add(objNode);
                        }
                    }
                }

                treLimit.SortCustom(strSelectedId);
            }
            else
            {
                aobjLimitNodes[0] = treLimit.FindNode("Node_Physical", false);
                aobjLimitNodes[1] = treLimit.FindNode("Node_Mental", false);
                aobjLimitNodes[2] = treLimit.FindNode("Node_Social", false);
                aobjLimitNodes[3] = treLimit.FindNode("Node_Astral", false);

                switch (notifyCollectionChangedEventArgs.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        {
                            foreach (LimitModifier objLimitModifier in notifyCollectionChangedEventArgs.NewItems)
                            {
                                int intTargetLimit = (int)Enum.Parse(typeof(LimitType), objLimitModifier.Limit);
                                TreeNode objParentNode = GetLimitModifierParentNode(intTargetLimit);
                                TreeNodeCollection lstParentNodeChildren = objParentNode.Nodes;
                                if (!lstParentNodeChildren.ContainsKey(objLimitModifier.DisplayName))
                                {
                                    TreeNode objNode = objLimitModifier.CreateTreeNode(cmsLimitModifier);
                                    int intNodesCount = lstParentNodeChildren.Count;
                                    int intTargetIndex = 0;
                                    for (; intTargetIndex < intNodesCount; ++intTargetIndex)
                                    {
                                        if (CompareTreeNodes.CompareText(lstParentNodeChildren[intTargetIndex], objNode) >= 0)
                                        {
                                            break;
                                        }
                                    }
                                    lstParentNodeChildren.Insert(intTargetIndex, objNode);
                                    objParentNode.Expand();
                                    treLimit.SelectedNode = objNode;
                                }
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        {
                            foreach (LimitModifier objLimitModifier in notifyCollectionChangedEventArgs.OldItems)
                            {
                                TreeNode objNode = treLimit.FindNodeByTag(objLimitModifier);
                                if (objNode != null)
                                {
                                    TreeNode objParent = objNode.Parent;
                                    objNode.Remove();
                                    if (objParent.Level == 0 && objParent.Nodes.Count == 0)
                                        objParent.Remove();
                                }
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        {
                            List<TreeNode> lstOldParentNodes = new List<TreeNode>();
                            foreach (LimitModifier objLimitModifier in notifyCollectionChangedEventArgs.OldItems)
                            {
                                TreeNode objNode = treLimit.FindNodeByTag(objLimitModifier);
                                if (objNode != null)
                                {
                                    lstOldParentNodes.Add(objNode.Parent);
                                    objNode.Remove();
                                }
                            }
                            foreach (LimitModifier objLimitModifier in notifyCollectionChangedEventArgs.NewItems)
                            {
                                int intTargetLimit = (int)Enum.Parse(typeof(LimitType), objLimitModifier.Limit);
                                TreeNode objParentNode = GetLimitModifierParentNode(intTargetLimit);
                                TreeNodeCollection lstParentNodeChildren = objParentNode.Nodes;
                                if (!lstParentNodeChildren.ContainsKey(objLimitModifier.DisplayName))
                                {
                                    TreeNode objNode = objLimitModifier.CreateTreeNode(cmsLimitModifier);
                                    int intNodesCount = lstParentNodeChildren.Count;
                                    int intTargetIndex = 0;
                                    for (; intTargetIndex < intNodesCount; ++intTargetIndex)
                                    {
                                        if (CompareTreeNodes.CompareText(lstParentNodeChildren[intTargetIndex], objNode) >= 0)
                                        {
                                            break;
                                        }
                                    }
                                    lstParentNodeChildren.Insert(intTargetIndex, objNode);
                                    objParentNode.Expand();
                                    treLimit.SelectedNode = objNode;
                                }
                            }
                            foreach (TreeNode objOldParentNode in lstOldParentNodes)
                            {
                                if (objOldParentNode.Level == 0 && objOldParentNode.Nodes.Count == 0)
                                    objOldParentNode.Remove();
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        {
                            RefreshLimitModifiers();
                        }
                        break;
                }
            }

            TreeNode GetLimitModifierParentNode(int intTargetLimit)
            {
                TreeNode objParentNode = aobjLimitNodes[intTargetLimit];
                if (objParentNode == null)
                {
                    switch (intTargetLimit)
                    {
                        case 0:
                            objParentNode = new TreeNode()
                            {
                                Tag = "Node_Physical",
                                Text = LanguageManager.GetString("Node_Physical", GlobalOptions.Language)
                            };
                            treLimit.Nodes.Insert(0, objParentNode);
                            break;
                        case 1:
                            objParentNode = new TreeNode()
                            {
                                Tag = "Node_Mental",
                                Text = LanguageManager.GetString("Node_Mental", GlobalOptions.Language)
                            };
                            treLimit.Nodes.Insert(aobjLimitNodes[0] == null ? 0 : 1, objParentNode);
                            break;
                        case 2:
                            objParentNode = new TreeNode()
                            {
                                Tag = "Node_Social",
                                Text = LanguageManager.GetString("Node_Social", GlobalOptions.Language)
                            };
                            treLimit.Nodes.Insert((aobjLimitNodes[0] == null ? 0 : 1) + (aobjLimitNodes[1] == null ? 0 : 1), objParentNode);
                            break;
                        case 3:
                            objParentNode = new TreeNode()
                            {
                                Tag = "Node_Astral",
                                Text = LanguageManager.GetString("Node_Astral", GlobalOptions.Language)
                            };
                            treLimit.Nodes.Add(objParentNode);
                            break;
                    }
                    objParentNode?.Expand();
                }
                return objParentNode;
            }
        }

        /// <summary>
        /// Edit and update a Limit Modifier.
        /// </summary>
        protected void UpdateLimitModifier()
        {
            if (treLimit.SelectedNode.Level <= 0) return;
            TreeNode objSelectedNode = treLimit.SelectedNode;
            string strGuid = (objSelectedNode?.Tag as IHasInternalId)?.InternalId ?? string.Empty;
            if (string.IsNullOrEmpty(strGuid) || strGuid.IsEmptyGuid())
                return;
            LimitModifier objLimitModifier = _characterObject.LimitModifiers.FindById(strGuid);
            //If the LimitModifier couldn't be found (Ie it comes from an Improvement or the user hasn't properly selected a treenode, fail out early.
            if (objLimitModifier == null)
            {
                MessageBox.Show(LanguageManager.GetString("Warning_NoLimitFound", GlobalOptions.Language));
                return;
            }
            using (frmSelectLimitModifier frmPickLimitModifier = new frmSelectLimitModifier(objLimitModifier, "Physical", "Mental", "Social"))
            {
                frmPickLimitModifier.ShowDialog(this);

                if (frmPickLimitModifier.DialogResult == DialogResult.Cancel)
                    return;

                //Remove the old LimitModifier to ensure we don't double up.
                _characterObject.LimitModifiers.Remove(objLimitModifier);
                // Create the new limit modifier.
                objLimitModifier = new LimitModifier(_characterObject);
                objLimitModifier.Create(frmPickLimitModifier.SelectedName, frmPickLimitModifier.SelectedBonus, frmPickLimitModifier.SelectedLimitType, frmPickLimitModifier.SelectedCondition);
                objLimitModifier.Guid = new Guid(strGuid);

                _characterObject.LimitModifiers.Add(objLimitModifier);

                MakeDirtyWithCharacterUpdate?.Invoke(null, null);
            }
        }

        private void LimitModifierCollectionChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            RefreshLimitModifiers(notifyCollectionChangedEventArgs);
        }
        #endregion
        #region Properties

        public ContextMenuStrip LimitContextMenuStrip => cmsLimitModifier;
        public TreeView LimitTreeView => treLimit;

        #endregion
    }
}
