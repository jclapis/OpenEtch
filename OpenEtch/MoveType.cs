﻿/* ========================================================================
 * Copyright (C) 2020 Joe Clapis.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * ======================================================================== */


namespace OpenEtch
{
    /// <summary>
    /// Represents the type of a <see cref="Path"/>.
    /// </summary>
    internal enum MoveType
    {
        /// <summary>
        /// This is a move for the pre-etch perimeter trace.
        /// </summary>
        Trace,

        /// <summary>
        /// This is a move between etch steps, when the laser is off.
        /// </summary>
        Travel,

        /// <summary>
        /// This is an etch move, when the laser is on.
        /// </summary>
        Etch
    }
}
