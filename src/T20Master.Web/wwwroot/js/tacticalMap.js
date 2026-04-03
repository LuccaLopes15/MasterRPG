// Tactical Map - Drag and Drop + Zoom/Pan
window.tacticalMap = {
    _dotNetRef: null,
    _isMaster: false,
    _draggingTokenId: null,
    _initialized: false,

    // Zoom/Pan state
    _zoom: 1,
    _panX: 0,
    _panY: 0,
    _isPanning: false,
    _panStartX: 0,
    _panStartY: 0,
    _panStartPanX: 0,
    _panStartPanY: 0,
    _container: null,
    _grid: null,
    _MIN_ZOOM: 0.5,
    _MAX_ZOOM: 3,
    _ZOOM_STEP: 0.15,

    init: function (dotNetRef, isMaster) {
        this._dotNetRef = dotNetRef;
        this._isMaster = isMaster;

        if (this._initialized) return;
        this._initialized = true;

        // Drag and drop - document-level delegation
        document.addEventListener('dragstart', (e) => {
            if (!this._isMaster) return;
            const token = e.target.closest('.map-token');
            if (!token) return;

            this._draggingTokenId = token.dataset.tokenId;
            e.dataTransfer.setData('text/plain', token.dataset.tokenId);
            e.dataTransfer.effectAllowed = 'move';
            setTimeout(() => token.classList.add('dragging'), 0);

            // Show movement range overlay
            const speed = parseFloat(token.dataset.speed || '0');
            const color = token.dataset.color || '#E76D25';
            if (speed > 0) {
                const cell = token.closest('.grid-cell');
                if (cell) {
                    const originX = parseInt(cell.dataset.x);
                    const originY = parseInt(cell.dataset.y);
                    const rangeInSquares = Math.floor(speed / 1.5);
                    this._showRangeOverlay(originX, originY, rangeInSquares, color);
                }
            }
        });

        document.addEventListener('dragover', (e) => {
            if (!this._isMaster || !this._draggingTokenId) return;
            const cell = e.target.closest('.grid-cell');
            if (!cell) return;
            e.preventDefault();
            e.dataTransfer.dropEffect = 'move';
        });

        document.addEventListener('drop', (e) => {
            if (!this._isMaster || !this._draggingTokenId) return;
            const cell = e.target.closest('.grid-cell');
            if (!cell) return;

            e.preventDefault();
            e.stopPropagation();

            const x = parseInt(cell.dataset.x);
            const y = parseInt(cell.dataset.y);
            const tokenId = this._draggingTokenId;
            this._draggingTokenId = null;

            this.wasDrop = true;
            this._clearRangeOverlay();
            if (this._dotNetRef) {
                this._dotNetRef.invokeMethodAsync('OnTokenDropped', tokenId, x, y);
            }
        });

        document.addEventListener('dragend', (e) => {
            this._draggingTokenId = null;
            this._clearRangeOverlay();
            const token = e.target.closest('.map-token');
            if (token) token.classList.remove('dragging');
        });

        // Zoom/Pan initialization
        this._initZoom();
    },

    _initZoom: function () {
        this._container = document.querySelector('.tactical-map-container');
        this._grid = document.querySelector('.tactical-grid');
        if (!this._container || !this._grid) return;

        // Wheel zoom
        this._container.addEventListener('wheel', (e) => {
            e.preventDefault();

            const rect = this._container.getBoundingClientRect();
            const mouseX = e.clientX - rect.left;
            const mouseY = e.clientY - rect.top;

            const oldZoom = this._zoom;
            this._zoom += (e.deltaY < 0 ? this._ZOOM_STEP : -this._ZOOM_STEP);
            this._zoom = Math.max(this._MIN_ZOOM, Math.min(this._MAX_ZOOM, this._zoom));

            // Zoom toward cursor position
            const zoomRatio = this._zoom / oldZoom;
            this._panX = mouseX - (mouseX - this._panX) * zoomRatio;
            this._panY = mouseY - (mouseY - this._panY) * zoomRatio;

            this._applyTransform(false);
        }, { passive: false });

        // Pan with middle mouse button or Ctrl+left click
        this._container.addEventListener('mousedown', (e) => {
            if (e.button === 1 || (e.button === 0 && e.ctrlKey)) {
                e.preventDefault();
                this._isPanning = true;
                this._panStartX = e.clientX;
                this._panStartY = e.clientY;
                this._panStartPanX = this._panX;
                this._panStartPanY = this._panY;
                this._container.style.cursor = 'grabbing';
            }
        });

        document.addEventListener('mousemove', (e) => {
            if (!this._isPanning) return;
            this._panX = this._panStartPanX + (e.clientX - this._panStartX);
            this._panY = this._panStartPanY + (e.clientY - this._panStartY);
            this._applyTransform(false);
        });

        document.addEventListener('mouseup', () => {
            if (this._isPanning) {
                this._isPanning = false;
                if (this._container) this._container.style.cursor = '';
            }
        });

        // Prevent context menu on middle click
        this._container.addEventListener('contextmenu', (e) => {
            if (e.button === 1) e.preventDefault();
        });
    },

    _applyTransform: function (smooth) {
        if (!this._grid) return;
        this._grid.style.transition = smooth ? 'transform 0.2s ease-out' : 'none';
        this._grid.style.transformOrigin = '0 0';
        this._grid.style.transform = `translate(${this._panX}px, ${this._panY}px) scale(${this._zoom})`;
    },

    zoomIn: function () {
        this._zoom = Math.min(this._MAX_ZOOM, this._zoom + this._ZOOM_STEP);
        this._applyTransform(true);
    },

    zoomOut: function () {
        this._zoom = Math.max(this._MIN_ZOOM, this._zoom - this._ZOOM_STEP);
        this._applyTransform(true);
    },

    resetView: function () {
        this._zoom = 1;
        this._panX = 0;
        this._panY = 0;
        this._applyTransform(true);
    },

    toggleFullscreen: function () {
        if (!this._container) return;
        if (document.fullscreenElement) {
            document.exitFullscreen();
        } else {
            this._container.requestFullscreen();
        }
    },

    _showRangeOverlay: function (originX, originY, range, color) {
        this._clearRangeOverlay();
        const grid = document.getElementById('tactical-grid');
        if (!grid) return;

        const cells = grid.querySelectorAll('.grid-cell');
        cells.forEach(cell => {
            const cx = parseInt(cell.dataset.x);
            const cy = parseInt(cell.dataset.y);
            // Euclidean distance for circular range
            const dist = Math.sqrt(Math.pow(cx - originX, 2) + Math.pow(cy - originY, 2));
            if (dist > 0 && dist <= range) {
                const overlay = document.createElement('div');
                overlay.className = 'range-overlay';
                overlay.style.background = color;
                cell.appendChild(overlay);
            }
        });
    },

    _clearRangeOverlay: function () {
        document.querySelectorAll('.range-overlay').forEach(el => el.remove());
    },

    // Called by Blazor to check if a drop just happened (to skip onclick)
    wasDrop: false,
    checkAndResetDrop: function () {
        var was = this.wasDrop;
        this.wasDrop = false;
        return was;
    },

    dispose: function () {
        this._dotNetRef = null;
        this._isMaster = false;
        this._draggingTokenId = null;
        this._initialized = false;
        this._zoom = 1;
        this._panX = 0;
        this._panY = 0;
    }
};
