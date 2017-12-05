﻿using System;
using System.Linq;
using umbraco.BusinessLogic.Actions;
using UIOMatic.Extensions;
using UIOMatic.Services;
using UIOMatic.Attributes;
using UIOMatic.Enums;
using UIOMatic.Interfaces;
using Umbraco.Core;
using Umbraco.Core.Services;
using Umbraco.Web.Models.Trees;
using Umbraco.Web.Mvc;
using Umbraco.Web.Trees;

namespace UIOMatic.Web.Controllers
{
    [Tree("uiomatic", "uiomatic", "Gestione tabelle")]
    [PluginController("UIOMatic")]
    public class UIOMaticTreeController : TreeController
    {
        private IUIOMaticObjectService _service;

        public UIOMaticTreeController()
        {
            _service = UIOMaticObjectService.Instance;
        }

        protected override TreeNodeCollection GetTreeNodes(string id, System.Net.Http.Formatting.FormDataCollection queryStrings)
        {
            var nodes = new TreeNodeCollection(); 
            var types = Helper.GetUIOMaticFolderTypes().OrderBy(x=> x.GetCustomAttribute<UIOMaticFolderAttribute>(false).Order);
            var groups = this.Services.UserService.GetUserById(this.Security.CurrentUser.Id).Groups.Select(x => x.Alias);

            foreach (var type in types)
            {
                var attri = (UIOMaticFolderAttribute)Attribute.GetCustomAttribute(type, typeof(UIOMaticFolderAttribute));
                if(attri == null)
                    continue;

                var alias = attri.Alias.IsNullOrWhiteSpace() ? type.Name : attri.Alias;
                if (attri.ParentAlias == id)
                {
                    var attri2 = attri as UIOMaticAttribute; 
                    if (attri2 != null)
                    {
                        if(attri2.HideFromTree)
                            continue;

                        if (attri2.UserGroup != null && !groups.Contains(attri2.UserGroup) && !groups.Contains("admin"))
                            continue;

                        // UIOMatic node
                        if (attri2.RenderType == UIOMaticRenderType.Tree)
                        {
                            // Tree node
                            var node = this.CreateTreeNode(
                                alias,
                                id,
                                queryStrings,
                                attri.FolderName,
                                attri.FolderIcon,
                                true,
                                "uiomatic");

                            nodes.Add(node);
                        }
                        else
                        {
                            // List view node
                            var node = this.CreateTreeNode(
                                alias,
                                id,
                                queryStrings,
                                attri.FolderName,
                                attri.FolderIcon,
                                false,
                                "uiomatic/uiomatic/list/" + alias);

                            node.SetContainerStyle();

                            nodes.Add(node);
                        }
                    }
                    else
                    {
                        // Just a folder
                        var node = this.CreateTreeNode(
                               attri.Alias,
                               id,
                               queryStrings,
                               attri.FolderName,
                               attri.FolderIcon,
                               true,
                               "uiomatic");

                        nodes.Add(node);
                    }
                }
                else if (id == alias)
                {
                    var attri2 = attri as UIOMaticAttribute;
                    if (attri2 != null)
                    {
                        if (attri2.HideFromTree)
                            continue;

                        if (attri2.UserGroup != null && !groups.Contains(attri2.UserGroup) && !groups.Contains("admin"))
                            continue;

                        var primaryKeyPropertyName = type.GetPrimaryKeyName();

                        if(attri2.RenderType == UIOMaticRenderType.Tree)
                        { 
                            // List nodes
                            foreach (dynamic item in _service.GetAll(type, attri2.SortColumn, attri2.SortOrder))
                            {
                                var node = CreateTreeNode(
                                    ((object)item).GetPropertyValue(primaryKeyPropertyName) + "?ta=" + id,
                                    id,
                                    queryStrings,
                                    item.ToString(),
                                    attri2.ItemIcon,
                                    false);

                                nodes.Add(node);
                            }
                        }
                    }
                }
            }

            return nodes;
        }

        protected override MenuItemCollection GetMenuForNode(string id, System.Net.Http.Formatting.FormDataCollection queryStrings)
        {
            var menu = new MenuItemCollection();

            var createText = Services.TextService.Localize("actions/" + ActionNew.Instance.Alias);
            var deleteText = Services.TextService.Localize("actions/" + ActionDelete.Instance.Alias);
            var refreshText = Services.TextService.Localize("actions/" + ActionRefresh.Instance.Alias);

            if (id == "-1")
            {
                menu.Items.Add<RefreshNode, ActionRefresh>(refreshText, true);
            }
            else
            {
                if (id.IndexOf("?") > 0)
                {
                    var typeAlias = id.Split('?')[1].Replace("ta=", "");
                    var type = Helper.GetUIOMaticTypeByAlias(typeAlias, true);
                    if (type != null)
                    {
                        var attri = type.GetCustomAttribute<UIOMaticAttribute>(true);
                        if (attri != null && !attri.ReadOnly)
                            menu.Items.Add<ActionDelete>(deleteText);
                    }
                }
                else
                {
                    var type = Helper.GetUIOMaticTypeByAlias(id, true);
                    if (type != null)
                    {
                        var attri = type.GetCustomAttribute<UIOMaticFolderAttribute>(true);
                        if (attri != null)
                        {
                            var attri2 = attri as UIOMaticAttribute;
                            if(attri2 != null)
                            { 
                                if (!attri2.ReadOnly)
                                    menu.Items.Add<CreateChildEntity, ActionNew>(createText);

                                if (attri2.RenderType == UIOMaticRenderType.Tree)
                                    menu.Items.Add<RefreshNode, ActionRefresh>(refreshText, true);
                            }
                            else
                            {
                                menu.Items.Add<RefreshNode, ActionRefresh>(refreshText, true);
                            }
                        }
                    }
                }


            }
            return menu;
        }

    }
}